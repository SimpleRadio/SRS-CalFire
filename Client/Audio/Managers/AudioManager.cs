using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Utility;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network.Client;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using FragLabs.Audio.Codecs;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Wave.WaveFormats;
using NLog;
using WPFCustomMessageBox;
using Application = FragLabs.Audio.Codecs.Opus.Application;
using ClientAudio = Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Models.ClientAudio;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Managers
{
    public class AudioManager : IHandle<SRClientUpdateMessage>
    {
        public static readonly int MIC_SAMPLE_RATE = 16000;
        public static readonly int MIC_INPUT_AUDIO_LENGTH_MS = 40;
        public static readonly int MIC_SEGMENT_FRAMES = MIC_SAMPLE_RATE / 1000 * MIC_INPUT_AUDIO_LENGTH_MS;
        public static readonly int OUTPUT_SAMPLE_RATE = 48000;
        public static readonly int OUTPUT_AUDIO_LENGTH_MS = 40;
        public static readonly int OUTPUT_SEGMENT_FRAMES = OUTPUT_SAMPLE_RATE / 1000 * OUTPUT_AUDIO_LENGTH_MS;
        public static readonly int JITTER_BUFFER = 50; //in milliseconds

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly AudioInputSingleton _audioInputSingleton = AudioInputSingleton.Instance;
        private readonly AudioOutputSingleton _audioOutputSingleton = AudioOutputSingleton.Instance;

        private readonly CachedAudioEffectProvider _cachedAudioEffectsProvider;

        private readonly ConcurrentDictionary<string, ClientAudioProvider> _clientsBufferedAudio =
            new();

        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

        private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;

        private readonly Queue<short> _micInputQueue = new(MIC_SEGMENT_FRAMES * 3);
        private readonly bool windowsN;

        private MixingSampleProvider _clientAudioMixer;

        //buffer for effects
        //plays in parallel with radio output buffer
        private RadioAudioProvider[] _effectsOutputBuffer;

        private OpusEncoder _encoder;

        private int _errorCount;

        private WasapiOut _micWaveOut;
        private BufferedWaveProvider _micWaveOutBuffer;

        private ClientAudioProvider _passThroughAudioProvider;
        private EventDrivenResampler _resampler;

        private float _speakerBoost = 1.0f;
        private Preprocessor _speex;

        //private Stopwatch _stopwatch = new();

        private UDPClientAudioProcessor _udpClientAudioProcessor;
        private UDPVoiceHandler _udpVoiceHandler;
        private VolumeSampleProviderWithPeak _volumeSampleProvider;

        private WasapiCapture _wasapiCapture;

        private WasapiOut _waveOut;
        //Stopwatch _stopwatch = new Stopwatch();

        private readonly object lockObj = new();


        public AudioManager(bool windowsN)
        {
            this.windowsN = windowsN;

            _cachedAudioEffectsProvider = CachedAudioEffectProvider.Instance;
        }

        public float MicMax { get; set; } = -100;
        public float SpeakerMax { get; set; } = -100;

        public float MicBoost { get; set; } = 1.0f;

        public float SpeakerBoost
        {
            get => _speakerBoost;
            set
            {
                _speakerBoost = value;
                if (_volumeSampleProvider != null) _volumeSampleProvider.Volume = value;
            }
        }

        public Task HandleAsync(SRClientUpdateMessage message, CancellationToken cancellationToken)
        {
            if (!message.Connected) RemoveClientBuffer(message.SrClient);

            return Task.CompletedTask;
        }

        public void InitWaveOut()
        {
            MMDevice speakers = null;
            if (_audioOutputSingleton.SelectedAudioOutput.Value == null)
                speakers = WasapiOut.GetDefaultAudioEndpoint();
            else
                speakers = (MMDevice)_audioOutputSingleton.SelectedAudioOutput.Value;

            _waveOut = new WasapiOut(speakers, AudioClientShareMode.Shared, true, 40, windowsN);

            //add final volume boost to all mixed audio
            _volumeSampleProvider = new VolumeSampleProviderWithPeak(_clientAudioMixer,
                peak => SpeakerMax = (float)VolumeConversionHelper.ConvertFloatToDB(peak));
            _volumeSampleProvider.Volume = SpeakerBoost;

            if (speakers.AudioClient.MixFormat.Channels == 1)
            {
                if (_volumeSampleProvider.WaveFormat.Channels == 2)
                    _waveOut.Init(_volumeSampleProvider.ToMono());
                else
                    //already mono
                    _waveOut.Init(_volumeSampleProvider);
            }
            else
            {
                if (_volumeSampleProvider.WaveFormat.Channels == 1)
                    _waveOut.Init(_volumeSampleProvider.ToStereo());
                else
                    //already stereo
                    _waveOut.Init(_volumeSampleProvider);
            }

            _waveOut.Play();
        }

        public void InitMicPassthrough()
        {
            MMDevice micOutput = null;
            if (_audioOutputSingleton.SelectedMicAudioOutput.Value != null)
            {
                micOutput = (MMDevice)_audioOutputSingleton.SelectedMicAudioOutput.Value;

                _passThroughAudioProvider = new ClientAudioProvider(true);
                _micWaveOut = new WasapiOut(micOutput, AudioClientShareMode.Shared, true, 40, windowsN);

                _micWaveOutBuffer = new BufferedWaveProvider(new WaveFormat(OUTPUT_SAMPLE_RATE, 16, 1));
                _micWaveOutBuffer.ReadFully = true;
                _micWaveOutBuffer.DiscardOnBufferOverflow = true;

                var sampleProvider = _micWaveOutBuffer.ToSampleProvider();

                if (micOutput.AudioClient.MixFormat.Channels == 1)
                {
                    if (sampleProvider.WaveFormat.Channels == 2)
                        _micWaveOut.Init(sampleProvider.ToMono());
                    else
                        //already mono
                        _micWaveOut.Init(sampleProvider);
                }
                else
                {
                    if (sampleProvider.WaveFormat.Channels == 1)
                        _micWaveOut.Init(sampleProvider.ToStereo());
                    else
                        //already stereo
                        _micWaveOut.Init(sampleProvider);
                }

                _micWaveOut.Play();
            }
        }

        public void InitEncodersSpeex()
        {
            //opus
            _encoder = OpusEncoder.Create(MIC_SAMPLE_RATE, 1, Application.Voip);
            _encoder.ForwardErrorCorrection = false;

            //speex
            _speex = new Preprocessor(MIC_SEGMENT_FRAMES, MIC_SAMPLE_RATE);
        }


        public void InitMicInput()
        {
            var device = (MMDevice)_audioInputSingleton.SelectedAudioInput.Value;

            if (device == null) device = WasapiCapture.GetDefaultCaptureDevice();

            device.AudioEndpointVolume.Mute = false;

            _wasapiCapture = new WasapiCapture(device, true);
            _wasapiCapture.ShareMode = AudioClientShareMode.Shared;
            _wasapiCapture.DataAvailable += WasapiCaptureOnDataAvailable;
            _wasapiCapture.RecordingStopped += WasapiCaptureOnRecordingStopped;

            _wasapiCapture.StartRecording();
        }

        public void StartEncoding(string guid, InputDeviceManager inputManager,
            IPEndPoint endPoint)
        {
            InitEncodersSpeex();

            try
            {
                _micInputQueue.Clear();

                InitMixers();

                InitAudioBuffers();

                //Audio manager should start / stop and cleanup based on connection successfull and disconnect
                //Should use listeners to synchronise all the state
                InitWaveOut();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error starting audio Output - Quitting! " + ex.Message);
                ShowOutputError("Problem Initialising Audio Output!");
                Environment.Exit(1);
            }

            try
            {
                InitMicPassthrough();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error starting mic audio Output - Quitting! " + ex.Message);

                ShowOutputError("Problem Initialising Mic Audio Output!");
                Environment.Exit(1);
            }

            if (_audioInputSingleton.MicrophoneAvailable)
                try
                {
                    InitMicInput();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error starting audio Input - Quitting! " + ex.Message);

                    ShowInputError("Problem initialising Audio Input!");

                    Environment.Exit(1);
                }


            //Start UDP handler
            _udpVoiceHandler =
                new UDPVoiceHandler(guid, endPoint);
            _udpVoiceHandler.Connect();

            _udpClientAudioProcessor = new UDPClientAudioProcessor(_udpVoiceHandler, this, guid);
            _udpClientAudioProcessor.Start();

            EventBus.Instance.SubscribeOnBackgroundThread(this);
        }

        private void WasapiCaptureOnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Logger.Error("Recording Stopped");
        }

        // private WaveFileWriter _beforeWaveFile;
        // private WaveFileWriter _afterFileWriter;
        private void WasapiCaptureOnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_resampler == null)
                //create and use in the same thread or COM issues
                _resampler = new EventDrivenResampler(windowsN, _wasapiCapture.WaveFormat,
                    new WaveFormat(MIC_SAMPLE_RATE, 16, 1));
            // _beforeWaveFile = new WaveFileWriter(@"C:\Temp\Test-Preview-Before.wav", new WaveFormat(AudioManager.MIC_SAMPLE_RATE, 16, 1));
            // _afterFileWriter = new WaveFileWriter(@"C:\Temp\Test-Preview-after.wav", new WaveFormat(AudioManager.OUTPUT_SAMPLE_RATE, 16, 1));

            if (e.BytesRecorded > 0)
            {
                //Logger.Info($"Time: {_stopwatch.ElapsedMilliseconds} - Bytes: {e.BytesRecorded}");
                var resampledPCM16Bit = _resampler.Resample(e.Buffer, e.BytesRecorded);

                // Logger.Info($"Time: {_stopwatch.ElapsedMilliseconds} - Bytes: {resampledPCM16Bit.Length}");
                //fill sound buffer
                for (var i = 0; i < resampledPCM16Bit.Length; i++) _micInputQueue.Enqueue(resampledPCM16Bit[i]);

                //read out the queue
                while (_micInputQueue.Count >= MIC_SEGMENT_FRAMES)
                {
                    var pcmShort = new short[MIC_SEGMENT_FRAMES];

                    for (var i = 0; i < MIC_SEGMENT_FRAMES; i++) pcmShort[i] = _micInputQueue.Dequeue();

                    try
                    {
                        //volume boost pre
                        for (var i = 0; i < pcmShort.Length; i++)
                            // n.b. no clipping test going on here
                            pcmShort[i] = (short)(pcmShort[i] * MicBoost);

                        //process with Speex
                        _speex.Process(new ArraySegment<short>(pcmShort));

                        float max = 0;
                        for (var i = 0; i < pcmShort.Length; i++)
                            //determine peak
                            if (pcmShort[i] > max)
                                max = pcmShort[i];
                        //convert to dB
                        MicMax = (float)VolumeConversionHelper.ConvertFloatToDB(max / 32768F);

                        var pcmBytes = new byte[pcmShort.Length * 2];
                        Buffer.BlockCopy(pcmShort, 0, pcmBytes, 0, pcmBytes.Length);

                        //encode as opus bytes
                        int len;
                        var buff = _encoder.Encode(pcmBytes, pcmBytes.Length, out len);

                        if (_udpVoiceHandler != null && buff != null && len > 0)
                        {
                            //create copy with small buffer
                            var encoded = new byte[len];

                            Buffer.BlockCopy(buff, 0, encoded, 0, len);

                            // Console.WriteLine("Sending: " + e.BytesRecorded);
                          
                            var clientAudio = _udpClientAudioProcessor?.Send(encoded, len);

                            // _beforeWaveFile.Write(pcmBytes, 0, pcmBytes.Length);

                            if (clientAudio != null && _micWaveOutBuffer != null)
                            {
                                //todo see if we can fix the resample / opus decode
                                //send audio so play over local too
                                var processedAudioBytes = _passThroughAudioProvider?.AddClientAudioSamples(clientAudio);
                                
                                //process bytes and add effects
                                if (processedAudioBytes?.Length > 0)
                                {
                                     // _afterFileWriter.Write(processedAudioBytes, 0, processedAudioBytes.Length);
                                    _micWaveOutBuffer?.AddSamples(processedAudioBytes, 0, processedAudioBytes.Length);
                                }
                                
                            }
                        }
                        else
                        {
                            Logger.Error(
                                $"Invalid Bytes for Encoding - {pcmShort.Length} should be {MIC_SEGMENT_FRAMES} ");
                        }

                        _errorCount = 0;
                    }
                    catch (Exception ex)
                    {
                        _errorCount++;
                        if (_errorCount < 10)
                            Logger.Error(ex, "Error encoding Opus! " + ex.Message);
                        else if (_errorCount == 10) Logger.Error(ex, "Final Log of Error encoding Opus! " + ex.Message);
                    }
                }
            }
        }

        private void ShowInputError(string message)
        {
            if (Environment.OSVersion.Version.Major == 10)
            {
                var messageBoxResult = CustomMessageBox.ShowYesNoCancel(
                    $"{message}\n\n" +
                    "If you are using Windows 10, this could be caused by your privacy settings (make sure to allow apps to access your microphone)." +
                    "\nAlternatively, try a different Input device and please post your client log to the support Discord server.",
                    "Audio Input Error",
                    "OPEN PRIVACY SETTINGS",
                    "JOIN DISCORD SERVER",
                    "CLOSE",
                    MessageBoxImage.Error);

                if (messageBoxResult == MessageBoxResult.Yes)
                    Process.Start("ms-settings:privacy-microphone");
                else if (messageBoxResult == MessageBoxResult.No) Process.Start("https://discord.gg/baw7g3t");
            }
            else
            {
                var messageBoxResult = CustomMessageBox.ShowYesNo(
                    $"{message}\n\n" +
                    "Try a different Input device and please post your client log to the support Discord server.",
                    "Audio Input Error",
                    "JOIN DISCORD SERVER",
                    "CLOSE",
                    MessageBoxImage.Error);

                if (messageBoxResult == MessageBoxResult.Yes) Process.Start("https://discord.gg/baw7g3t");
            }
        }

        private void ShowOutputError(string message)
        {
            var messageBoxResult = CustomMessageBox.ShowYesNo(
                $"{message}\n\n" +
                "Try a different output device and please post your client log to the support Discord server.",
                "Audio Output Error",
                "JOIN DISCORD SERVER",
                "CLOSE",
                MessageBoxImage.Error);

            if (messageBoxResult == MessageBoxResult.Yes) Process.Start("https://discord.gg/baw7g3t");
        }

        private void InitMixers()
        {
            _clientAudioMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(OUTPUT_SAMPLE_RATE, 2));
            _clientAudioMixer.ReadFully = true;
        }

        private void InitAudioBuffers()
        {
            _effectsOutputBuffer = new RadioAudioProvider[_clientStateSingleton.PlayerUnitState.Radios.Count];

            for (var i = 0; i < _clientStateSingleton.PlayerUnitState.Radios.Count; i++)
            {
                _effectsOutputBuffer[i] = new RadioAudioProvider(OUTPUT_SAMPLE_RATE);
                _clientAudioMixer.AddMixerInput(_effectsOutputBuffer[i].VolumeSampleProvider);
            }
        }


        public void PlaySoundEffectStartReceive(int transmitOnRadio, bool encrypted, float volume,
            Modulation modulation)
        {
            if (!_globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_Start))
                return;

            var midsTone =
                _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.MIDSRadioEffect);

            if (modulation == Modulation.MIDS && midsTone)
                //no tone for MIDS
                return;

            var _effectsBuffer = _effectsOutputBuffer[transmitOnRadio];

            if (encrypted &&
                _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioEncryptionEffects))
            {
                _effectsBuffer.VolumeSampleProvider.Volume = volume;

                var effect = _cachedAudioEffectsProvider.KY58EncryptionEndTone;
                if (effect.Loaded)
                    _effectsBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
            else
            {
                _effectsBuffer.VolumeSampleProvider.Volume = volume;

                var effect = _cachedAudioEffectsProvider.SelectedRadioTransmissionStartEffect;
                if (effect.Loaded)
                    _effectsBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
        }

        public void PlaySoundEffectStartTransmit(int transmitOnRadio, bool encrypted, float volume,
            Modulation modulation)
        {
            if (!_globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_Start))
                return;

            var _effectBuffer = _effectsOutputBuffer[transmitOnRadio];

            var midsTone =
                _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.MIDSRadioEffect);

            if (encrypted &&
                _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioEncryptionEffects))
            {
                _effectBuffer.VolumeSampleProvider.Volume = volume;
                var effect = _cachedAudioEffectsProvider.KY58EncryptionTransmitTone;
                if (effect.Loaded)
                    _effectBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
            else if (modulation == Modulation.MIDS && midsTone)
            {
                _effectBuffer.VolumeSampleProvider.Volume = volume;
                var effect = _cachedAudioEffectsProvider.MIDSTransmitTone;
                if (effect.Loaded)
                    _effectBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
            else
            {
                _effectBuffer.VolumeSampleProvider.Volume = volume;
                var effect = _cachedAudioEffectsProvider.SelectedRadioTransmissionStartEffect;
                if (effect.Loaded)
                    _effectBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
        }


        public void PlaySoundEffectEndReceive(int transmitOnRadio, float volume, Modulation modulation)
        {
            if (!_globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End))
                return;

            var midsTone =
                _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.MIDSRadioEffect);

            if (modulation == Modulation.MIDS && midsTone)
            {
                //end receive tone for MIDS
                var effectsBuffer = _effectsOutputBuffer[transmitOnRadio];

                effectsBuffer.VolumeSampleProvider.Volume = volume;
                var effect = _cachedAudioEffectsProvider.MIDSEndTone;
                if (effect.Loaded)
                    effectsBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
            else
            {
                var effectsBuffer = _effectsOutputBuffer[transmitOnRadio];

                effectsBuffer.VolumeSampleProvider.Volume = volume;
                var effect = _cachedAudioEffectsProvider.SelectedRadioTransmissionEndEffect;
                if (effect.Loaded)
                    effectsBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
        }

        public void PlaySoundEffectEndTransmit(int transmitOnRadio, float volume, Modulation modulation)
        {
            if (!_globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_End))
                return;

            var midsTone =
                _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.MIDSRadioEffect);
            var _effectBuffer = _effectsOutputBuffer[transmitOnRadio];

            if (modulation == Modulation.MIDS && midsTone)
            {
                _effectBuffer.VolumeSampleProvider.Volume = volume;
                var effect = _cachedAudioEffectsProvider.MIDSEndTone;
                if (effect.Loaded)
                    _effectBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
            else
            {
                _effectBuffer.VolumeSampleProvider.Volume = volume;
                var effect = _cachedAudioEffectsProvider.SelectedRadioTransmissionEndEffect;
                if (effect.Loaded)
                    _effectBuffer.AddAudioSamples(
                        effect.AudioEffectBytes,
                        transmitOnRadio);
            }
        }

        public void StopEncoding()
        {
            lock (lockObj)
            {
                //TODO
                //Stop input handler
                _udpClientAudioProcessor?.Stop();
                _udpClientAudioProcessor = null;

                _wasapiCapture?.StopRecording();
                _wasapiCapture?.Dispose();
                _wasapiCapture = null;

                _resampler?.Dispose(true);
                _resampler = null;

                //Debug Wav
                // _afterFileWriter?.Close();
                // _afterFileWriter?.Dispose();
                // _beforeWaveFile?.Close();
                // _beforeWaveFile?.Dispose();

                _waveOut?.Stop();
                _waveOut?.Dispose();
                _waveOut = null;

                _micWaveOut?.Stop();
                _micWaveOut?.Dispose();
                _micWaveOut = null;

                _volumeSampleProvider = null;
                _clientAudioMixer?.RemoveAllMixerInputs();
                _clientAudioMixer = null;

                _clientsBufferedAudio.Clear();

                _encoder?.Dispose();
                _encoder = null;

                if (_udpVoiceHandler != null)
                {
                    _udpVoiceHandler.RequestStop();
                    _udpVoiceHandler = null;
                }

                _speex?.Dispose();
                _speex = null;

                SpeakerMax = -100;
                MicMax = -100;

                _effectsOutputBuffer = null;
                EventBus.Instance.Unsubcribe(this);
            }
        }


        public void AddClientAudio(ClientAudio audio)
        {
            //sort out effects!

            //16bit PCM Audio
            //TODO: Clean  - remove if we havent received audio in a while?
            // If we have recieved audio, create a new buffered audio and read it
            ClientAudioProvider client = null;
            if (_clientsBufferedAudio.ContainsKey(audio.OriginalClientGuid))
            {
                client = _clientsBufferedAudio[audio.OriginalClientGuid];
            }
            else
            {
                client = new ClientAudioProvider();
                _clientsBufferedAudio[audio.OriginalClientGuid] = client;

                _clientAudioMixer.AddMixerInput(client.SampleProvider);
            }

            client.AddClientAudioSamples(audio);
        }

        private void RemoveClientBuffer(SRClientBase srClient)
        {
            //TODO test this
            ClientAudioProvider clientAudio = null;
            _clientsBufferedAudio.TryRemove(srClient.ClientGuid, out clientAudio);

            if (clientAudio == null) return;

            try
            {
                _clientAudioMixer.RemoveMixerInput(clientAudio.SampleProvider);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error removing client input");
            }
        }
    }
}