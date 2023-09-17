using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Android.Content;
using Android.Media;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Opus.Core;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Network.Client;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Singleton;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Utility;
using Ciribob.SRS.Common.Network.Client;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using Java.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLog;
using Application = Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Opus.Application;
using LogManager = NLog.LogManager;
using Process = Android.OS.Process;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;

public class SRSConnectionManager : IHandle<TCPClientStatusMessage>, IHandle<SRClientUpdateMessage>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static readonly int MIC_INPUT_AUDIO_LENGTH_MS = 40;

    public static readonly int MIC_SEGMENT_FRAMES_BYTES =
        Constants.MIC_SAMPLE_RATE / 1000 * MIC_INPUT_AUDIO_LENGTH_MS * 2; //2 because its bytes not shorts

    private readonly ConcurrentDictionary<string, ClientAudioProvider> _clientsBufferedAudio = new();

    private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

    private readonly CancellationTokenSource _stopFlag = new();
    private readonly string Guid = ShortGuid.NewGuid();

    private readonly object lockob = new();
    
    private UDPClientAudioProcessor _clientAudioProcessor;
    private SRSMixingSampleProvider _finalMixdown;

    private List<RadioMixingProvider> _radioMixingProvider;

    private float _speakerBoost = 1.0f;

    private IPEndPoint endPoint;

    private bool stop;

    private UDPVoiceHandler udpVoiceHandler;
    private TCPClientHandler _srsClientSyncHandler;
    private static SRSConnectionManager _instance;
    private static object _lock = new();

    public static SRSConnectionManager Instance
    {
        get
        {
            if (_instance == null)
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new SRSConnectionManager();
                }

            return _instance;
        }
    }

    public bool TCPConnected
    {
        get
        {
            if (_srsClientSyncHandler != null)
            {
                return _srsClientSyncHandler.TCPConnected;
            }

            return false;
        }
    }
    
    public bool UDPConnected
    {
        get
        {
            if (udpVoiceHandler != null)
            {
                return udpVoiceHandler.Ready;
            }

            return false;
        }
    }

    private SRSConnectionManager()
    {
    }

    public Task HandleAsync(SRClientUpdateMessage message, CancellationToken cancellationToken)
    {
        if (!message.Connected) RemoveClientBuffer(message.SrClient);

        return Task.CompletedTask;
    }

    public Task HandleAsync(TCPClientStatusMessage message, CancellationToken cancellationToken)
    {
        if (message.Connected)
            ReadyToSend();
        else
            Disconnected();

        return Task.CompletedTask;
    }

    private void InitMixers()
    {
        _finalMixdown?.RemoveAllMixerInputs();
        
        _finalMixdown =
            new SRSMixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(Constants.OUTPUT_SAMPLE_RATE, 2));
        _finalMixdown.ReadFully = true;

        _radioMixingProvider = new List<RadioMixingProvider>();
        for (var i = 0; i < _clientStateSingleton.PlayerUnitState.Radios.Count; i++)
        {
            var mix = new RadioMixingProvider(WaveFormat.CreateIeeeFloatWaveFormat(Constants.OUTPUT_SAMPLE_RATE, 2), i);
            _radioMixingProvider.Add(mix);
            _finalMixdown.AddMixerInput(mix);
        }
    }

    public void StartAndConnect(IPEndPoint ipEndpoint)
    {
        endPoint = ipEndpoint;

        lock (lockob)
        {
            InitMixers();

            EventBus.Instance.SubscribeOnUIThread(this);

            //Connect to TCP and UDP

            _srsClientSyncHandler =
                new TCPClientHandler(Guid, ClientStateSingleton.Instance.PlayerUnitState.PlayerUnitStateBase);

            _srsClientSyncHandler.TryConnect(endPoint);
        }
    }


    public void StopEncoding()
    {
        lock (lockob)
        {
            if (udpVoiceHandler != null)
            {
                EventBus.Instance.Unsubcribe(this);
                stop = true;
                _stopFlag.Cancel();

                _clientAudioProcessor?.Stop();
                _clientAudioProcessor = null;
            
                udpVoiceHandler?.RequestStop();
                udpVoiceHandler = null;
            }
        }
    }

    private void ReadyToSend()
    {
        if (udpVoiceHandler == null)
        {
            Logger.Info($"Connecting UDP VoIP {endPoint}");
            udpVoiceHandler = new UDPVoiceHandler(Guid, endPoint);
            udpVoiceHandler.Connect();

            _clientAudioProcessor = new UDPClientAudioProcessor(udpVoiceHandler, this, Guid);
            _clientAudioProcessor.Start();

            new Thread(SendAudio).Start();
            new Thread(ReceiveAudio).Start();
        }
    }

    private void SpeakerPhoneEnable()
    {
        // Get an AudioManager instance
        // AudioManager audioManager = AudioManager.FromContext(Platform.AppContext);
        // AudioDeviceInfo speakerDevice = null;
        // var devices = audioManager.AvailableCommunicationDevices;
        //
        // foreach (var device in devices) {
        //     if (device.Type == AudioDeviceType.BuiltinSpeakerSafe) {
        //         speakerDevice = device;
        //         break;
        //     }
        // }
        // if (speakerDevice != null) {
        //     // Turn speakerphone ON.
        //     bool result = audioManager.SetCommunicationDevice(speakerDevice);
        //     if (!result) {
        //         // Handle error.
        //     }
        //     // // Turn speakerphone OFF.
        //     // audioManager.clearCommunicationDevice();
        // }
        
      

    }
    private void ReceiveAudio()
    {
       
        SpeakerPhoneEnable();
        SetAudioFocus();
        
        var _audioPlayer = new AudioTrack.Builder()
            .SetAudioAttributes(new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Media)
                .SetContentType(AudioContentType.Music)
                .Build())
            .SetAudioFormat(new AudioFormat.Builder()
                .SetEncoding(Encoding.PcmFloat)
                .SetSampleRate(Constants.OUTPUT_SAMPLE_RATE)
                .SetChannelMask(ChannelOut.Stereo)
                .Build())
            .SetBufferSizeInBytes(AudioTrack.GetMinBufferSize(Constants.OUTPUT_SAMPLE_RATE, ChannelOut.Stereo,
                Encoding.PcmFloat)) // Added artibrary * 10 ?
            .SetTransferMode(AudioTrackMode.Stream)
            .SetPerformanceMode(AudioTrackPerformanceMode.LowLatency)
            .Build();

        _audioPlayer.Play();

        var buffer =
            new float[Constants.OUTPUT_SAMPLE_RATE / 1000 * Constants.OUTPUT_AUDIO_LENGTH_MS * 2 * 16];

        Process.SetThreadPriority(global::Android.OS.ThreadPriority.Audio);
       // Thread.CurrentThread.Priority = ThreadPriority.Highest;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        long lastRead = 0;

        while (!stop)
            try
            {
                var current = stopwatch.ElapsedMilliseconds;
                
                var floatsRequired =
                    (current - lastRead) * (Constants.OUTPUT_SAMPLE_RATE / 1000) *
                    2; //Stereo samples * milliseconds

                lastRead = current;
                

                if (floatsRequired > 0 && floatsRequired < buffer.Length)
                {
                    var read = _finalMixdown.Read(buffer, 0,
                        (int)floatsRequired);
                    _audioPlayer?.Write(buffer, 0, read, WriteMode.Blocking);
                    Array.Clear(buffer);

                    // Logger.Info(
                    //     $"floats required {floatsRequired} -  {read} floats {read / 2 / (Constants.OUTPUT_SAMPLE_RATE / 1000)}ms");
                }


                Thread.Yield();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,"Error in Audio Thread!");
                break;
            }
        
        
        _audioPlayer.Stop();
        _audioPlayer.Release();
        
    }

    private void SetAudioFocus()
    {
        //AudioFocusRequestClass.Builder
        var builder = new AudioFocusRequestClass.Builder(AudioFocus.Gain);
        builder.SetFocusGain(AudioFocus.Gain);
        builder.SetWillPauseWhenDucked(false);
        var res = AudioManager.FromContext(Platform.AppContext).RequestAudioFocus(builder.Build());

        if (res == AudioFocusRequest.Granted)
        {
            //TODO
        }
    }

    private void SendAudio()
    {
        //Start
        var opusEncoder = OpusEncoder.Create(Constants.MIC_SAMPLE_RATE, 1,
            Application.Voip);
        opusEncoder.ForwardErrorCorrection = false;

        var audioBuffer = new byte[100000];
        var _audioRecorder = new AudioRecord(
            // Hardware source of recording.
            AudioSource.Default,
            // Frequency
            Constants.MIC_SAMPLE_RATE,
            // Mono or stereo
            ChannelIn.Mono,
            // Audio encoding
            Encoding.Pcm16bit,
            // Length of the audio clip.
            audioBuffer.Length
        );

        _audioRecorder.StartRecording();

        //TODO make bluetooth work
        //try
        //{
        //    var audioManager = (AudioManager)Android.App.Application.Context.GetSystemService(Context.AudioService);
        //    audioManager.BluetoothScoOn = true;
        //    audioManager.StartBluetoothSco();
        //}
        //catch (System.Exception ex)
        //{
        //    // Handle exception gently
        //}

        var recorderBuffer = new byte[MIC_SEGMENT_FRAMES_BYTES];

        while (!stop)
        {
            //Read the input
            var read = _audioRecorder.Read(recorderBuffer, 0, MIC_SEGMENT_FRAMES_BYTES, 0);

            if (read == MIC_SEGMENT_FRAMES_BYTES)
            {
                var encodedBytes = opusEncoder.Encode(recorderBuffer, MIC_SEGMENT_FRAMES_BYTES, out var encodedLength);

                var toSend = new byte[encodedLength];

                Buffer.BlockCopy(encodedBytes, 0, toSend, 0, encodedLength);

                _clientAudioProcessor?.Send(toSend, true);
            }
        }
        
        _audioRecorder.Stop();
        _audioRecorder.Release();
        
        opusEncoder?.Dispose();
        opusEncoder = null;
    }

    private void Disconnected()
    {
        stop = true;

        StopEncoding();
    }

    public void PlaySoundEffectEndTransmit(int sendingOn, float radioVolume, Modulation radioModulation)
    {
        _radioMixingProvider[sendingOn]?.PlaySoundEffectEndTransmit(radioVolume, radioModulation);
    }

    public void PlaySoundEffectStartTransmit(int sendingOn, bool encrypted, float volume, Modulation modulation)
    {
        _radioMixingProvider[sendingOn]?.PlaySoundEffectStartTransmit(encrypted, volume, modulation);
    }

    public void AddClientAudio(ClientAudio audio)
    {
        //sort out effects!
        //16bit PCM Audio
        //TODO: Clean  - remove if we havent received audio in a while?
        // If we have recieved audio, create a new buffered audio and read it
        ClientAudioProvider client = null;
        if (_clientsBufferedAudio.TryGetValue(audio.OriginalClientGuid, out var value))
        {
            client = value;
        }
        else
        {
            client = new ClientAudioProvider();
            _clientsBufferedAudio[audio.OriginalClientGuid] = client;

            foreach (var mixer in _radioMixingProvider) mixer.AddMixerInput(client);
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
            foreach (var mixer in _radioMixingProvider) mixer.RemoveMixerInput(clientAudio);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error removing client input");
        }

        //TODO for later
        //MAKE SURE TO INIT THE MIXERS - CLEAR WHEN APPROPRIATE
        //ALSO INIT THE _clientsBufferedAudio and also CLEAR AS APPROPRIATE
        //THE AUDIO writer should also calculate time between the last sleep and fill in the audio as well as appropriate (rather than assuming 40ms perfect)
        //VOX detection might be possible to add if I can find the right DLLS
        //FOR TESTING - need to set a radio on the singleton as well
    }
}