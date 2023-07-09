using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Android.Media;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Opus.Core;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Network.Client;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Singleton;
using Ciribob.SRS.Common.Network.Client;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLog;
using Application = Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Opus.Application;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;

public class SRSAudioManager : IHandle<TCPClientStatusMessage>, IHandle<PTTState>, IHandle<SRClientUpdateMessage>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static readonly int MIC_INPUT_AUDIO_LENGTH_MS = 40;

    public static readonly int MIC_SEGMENT_FRAMES_BYTES =
        Constants.MIC_SAMPLE_RATE / 1000 * MIC_INPUT_AUDIO_LENGTH_MS * 2; //2 because its bytes not shorts

    private readonly Queue<byte> _micInputQueue = new(MIC_SEGMENT_FRAMES_BYTES * 3);

    private readonly CancellationTokenSource _stopFlag = new();


    private readonly string Guid = ShortGuid.NewGuid();
    //private readonly CircularBuffer _circularBuffer = new CircularBuffer();

    private readonly object lockob = new();
    private AudioTrack _audioPlayer;
    private AudioRecord _audioRecorder;
    private OpusDecoder _decoder;
    private OpusEncoder _encoder;

    private float _speakerBoost = 1.0f;

    private IPEndPoint endPoint;

    private bool stop;

    private UDPVoiceHandler udpVoiceHandler;
    private UDPClientAudioProcessor _clientAudioProcessor;
    
    private readonly ConcurrentDictionary<string, ClientAudioProvider> _clientsBufferedAudio = new();

    private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

    private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;
    
    private readonly ClientEffectsPipeline _clientEffectsPipeline;
    
    private List<RadioMixingProvider> _radioMixingProvider;
    private SRSMixingSampleProvider _finalMixdown;


    public bool PTTPressed { get; set; }

    public SRSAudioManager()
    {
        _clientEffectsPipeline = new ClientEffectsPipeline();
    }
    
    private void InitMixers()
    {
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
    
    public Task HandleAsync(SRClientUpdateMessage message, CancellationToken cancellationToken)
    {
        if (!message.Connected) RemoveClientBuffer(message.SrClient);

        return Task.CompletedTask;
    }


    public Task HandleAsync(PTTState message, CancellationToken cancellationToken)
    {
        PTTPressed = message.PTTPressed;

        if (_clientAudioProcessor != null)
        {
            _clientAudioProcessor.PTT = PTTPressed;
        }

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

    public void StartPreview(IPEndPoint ipEndpoint)
    {
        endPoint = ipEndpoint;

        lock (lockob)
        {
            InitMixers();
            
            EventBus.Instance.SubscribeOnUIThread(this);
            //opus
            _encoder = OpusEncoder.Create(Constants.MIC_SAMPLE_RATE, 1,
                Application.Voip);
            _encoder.ForwardErrorCorrection = false;
            _decoder = OpusDecoder.Create(Constants.OUTPUT_SAMPLE_RATE, 1);
            _decoder.ForwardErrorCorrection = false;
            _decoder.MaxDataBytes = Constants.OUTPUT_SAMPLE_RATE * 4;

            //Connect to TCP and UDP

            var gameState = new PlayerUnitStateBase();
            gameState.Name = "MOBILE";
            gameState.UnitId = 100000000;
            gameState.Radios = new List<RadioBase>();
            gameState.Radios.Add(new RadioBase
            {
                Modulation = Modulation.DISABLED
            });
            gameState.Radios.Add(new RadioBase
            {
                Modulation = Modulation.AM,
                Freq = 1.51e+8d
            });

            var srsClientSyncHandler = new TCPClientHandler(Guid, gameState);

            srsClientSyncHandler.TryConnect(endPoint);
        }
    }


    public void StopEncoding()
    {
        if (!stop)
            lock (lockob)
            {
                EventBus.Instance.Unsubcribe(this);
                stop = true;
                _stopFlag.Cancel();

                _audioPlayer?.Stop();
                _audioRecorder?.Stop();

                _audioPlayer?.Release();
                _audioRecorder?.Release();

                _encoder?.Dispose();
                _encoder = null;

                _decoder?.Dispose();
                _decoder = null;

                _audioPlayer = null;
                _audioRecorder = null;
                
                _clientAudioProcessor?.Stop();
                _clientAudioProcessor = null;
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

    private void ReceiveAudio()
    {
        _audioPlayer = new AudioTrack.Builder()
            .SetAudioAttributes(new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.VoiceCommunication)
                .SetContentType(AudioContentType.Speech)
                .Build())
            .SetAudioFormat(new AudioFormat.Builder()
                .SetEncoding(Encoding.PcmFloat)
                .SetSampleRate(Constants.OUTPUT_SAMPLE_RATE)
                .SetChannelMask(ChannelOut.Stereo)
                .Build())
            .SetBufferSizeInBytes(AudioTrack.GetMinBufferSize(Constants.OUTPUT_SAMPLE_RATE, ChannelOut.Stereo,
                Encoding.PcmFloat)*16) // Added artibrary * 10 ?
            .SetTransferMode(AudioTrackMode.Stream)
            .Build();

        _audioPlayer.Play();

        float[] buffer =
            new float[((Constants.OUTPUT_SAMPLE_RATE / 1000) * Constants.OUTPUT_AUDIO_LENGTH_MS * 2 * 16 )];
            
  
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
       
        while (!stop)
            try
            {
                long floatsRequired = stopwatch.ElapsedMilliseconds * (Constants.OUTPUT_SAMPLE_RATE/1000) * 2; //Stereo samples * milliseconds
                stopwatch.Restart();

                if (floatsRequired > 0 && floatsRequired < buffer.Length)
                {
                    int read = _finalMixdown.Read(buffer, 0,
                        (int)floatsRequired); 
                    _audioPlayer.Write(buffer, 0, read, WriteMode.Blocking);
                    Array.Clear(buffer);
             //       Logger.Info($"floats required {read} floats {read/2/(Constants.OUTPUT_SAMPLE_RATE/1000)}ms");
                }
                Thread.Sleep(1);
            }
            catch (Exception ex)
            {
                return;
            }
    }

    private void SendAudio()
    {
        //Start

        var audioBuffer = new byte[100000];
        _audioRecorder = new AudioRecord(
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
        var encryptionBytes = new byte[1];
        var modulationBytes = new byte[1];
        modulationBytes[0] = (byte)Modulation.AM;

        double[] frequencies = { 1.51e+8d };

        while (!stop)
        {
            //Read the input
            var read = _audioRecorder.Read(recorderBuffer, 0, MIC_SEGMENT_FRAMES_BYTES, 0);

            if (read == MIC_SEGMENT_FRAMES_BYTES) 
            {
                var encodedBytes = _encoder.Encode(recorderBuffer, MIC_SEGMENT_FRAMES_BYTES, out var encodedLength);

                var toSend = new byte[encodedLength];

                Buffer.BlockCopy(encodedBytes, 0, toSend, 0, encodedLength);

                _clientAudioProcessor.PTT = PTTPressed;
                _clientAudioProcessor?.Send(toSend, encodedLength, true);

               //  var udpVoicePacket = new UDPVoicePacket
               //  {
               //      AudioPart1Bytes = toSend,
               //      AudioPart1Length = (ushort)encodedLength,
               //      Frequencies = frequencies,
               //      UnitId = 100000,
               //      Encryptions = encryptionBytes,
               //      Modulations = modulationBytes
               //  };
               //
               // udpVoiceHandler.Send(udpVoicePacket);
              }
        }
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
        if (_clientsBufferedAudio.ContainsKey(audio.OriginalClientGuid))
        {
            client = _clientsBufferedAudio[audio.OriginalClientGuid];
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
