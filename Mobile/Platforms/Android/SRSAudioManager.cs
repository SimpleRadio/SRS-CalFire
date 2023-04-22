using Android.Media;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Opus.Core;
using Application = Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Opus.Application;
using Ciribob.SRS.Common.Network.Client;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using NLog;
using Encoding = Android.Media.Encoding;
using LogManager = NLog.LogManager;
using Ciribob.SRS.Common.Network.Models;
using System.Net;

namespace Ciribob.SRS.Mobile.Client
{
    public class SRSAudioManager:IHandle<TCPClientStatusMessage>, IHandle<PTTState>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly int MIC_INPUT_AUDIO_LENGTH_MS = 40;
        public static readonly int MIC_SEGMENT_FRAMES_BYTES = Constants.MIC_SAMPLE_RATE / 1000 * MIC_INPUT_AUDIO_LENGTH_MS * 2; //2 because its bytes not shor
        private readonly Queue<byte> _micInputQueue = new(MIC_SEGMENT_FRAMES_BYTES * 3);
        private OpusDecoder _decoder;
        private OpusEncoder _encoder;

        private float _speakerBoost = 1.0f;
        //private readonly CircularBuffer _circularBuffer = new CircularBuffer();

        private readonly object lockob = new();
        private AudioRecord _audioRecorder;
        private AudioTrack _audioPlayer;

        private bool stop = false;

        private UDPVoiceHandler udpVoiceHandler;

        private IPEndPoint endPoint;


        private readonly string Guid = ShortGuid.NewGuid();

        private readonly CancellationTokenSource _stopFlag = new();

        public bool PTTPressed { get; set; }

        public void StartPreview(IPEndPoint ipEndpoint)
        {
            endPoint = ipEndpoint;

            lock (lockob)
            {
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
                gameState.Radios.Add(new RadioBase()
                {
                    Modulation = Modulation.DISABLED

                });
                gameState.Radios.Add(new RadioBase()
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
            {
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
                    .SetEncoding(Encoding.Pcm16bit)
                    .SetSampleRate(Constants.OUTPUT_SAMPLE_RATE)
                    .SetChannelMask(ChannelOut.Mono)
                    .Build())
                .SetBufferSizeInBytes(AudioTrack.GetMinBufferSize(Constants.OUTPUT_SAMPLE_RATE, ChannelOut.Mono, Encoding.PcmFloat))
                .SetTransferMode(AudioTrackMode.Stream)
                .Build();

            _audioPlayer.Play();

            while (!stop)
            {
                try
                {
                    var encodedOpusAudio = new byte[0];
                    udpVoiceHandler.EncodedAudio.TryTake(out encodedOpusAudio, 100000, _stopFlag.Token);


                    if (encodedOpusAudio != null
                        && encodedOpusAudio.Length >=
                        UDPVoicePacket.PacketHeaderLength + UDPVoicePacket.FixedPacketLength +
                        UDPVoicePacket.FrequencySegmentLength)
                    {

                        //Decode bytes
                        var udpVoicePacket = UDPVoicePacket.DecodeVoicePacket(encodedOpusAudio);

                        if (udpVoicePacket != null)
                        {

                            byte[] decodedBytes = _decoder.Decode(udpVoicePacket.AudioPart1Bytes, udpVoicePacket.AudioPart1Length, out int decodedLength);

                            if (decodedLength > 0)
                            {
                                _audioPlayer.Write(decodedBytes, 0, decodedLength, WriteMode.NonBlocking);
                            }
                         
                        }
                    }

                }
                catch (OperationCanceledException ex)
                {
                    return;
                }
            }
        }

        private void SendAudio()
        {
          
            //Start

            byte[] audioBuffer = new byte[100000];
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

            byte[] recorderBuffer = new byte[MIC_SEGMENT_FRAMES_BYTES];
            byte[] encryptionBytes = new byte[1];
            byte[] modulationBytes = new byte[1];
            modulationBytes[0] = (byte)Modulation.AM;

            double[] frequencies = new[] { 1.51e+8d };

            while (!stop)
            {
                //Read the input
                int read = _audioRecorder.Read(recorderBuffer, 0, SRSAudioManager.MIC_SEGMENT_FRAMES_BYTES, 0);

                if (read == SRSAudioManager.MIC_SEGMENT_FRAMES_BYTES)
                {
                    if (PTTPressed)
                    {
                        byte[] encodedBytes = _encoder.Encode(recorderBuffer, SRSAudioManager.MIC_SEGMENT_FRAMES_BYTES, out int encodedLength);

                        byte[] toSend = new byte[encodedLength];

                        Buffer.BlockCopy(encodedBytes, 0, toSend, 0, encodedLength);

                        var udpVoicePacket = new UDPVoicePacket
                        {
                            AudioPart1Bytes = toSend,
                            AudioPart1Length = (ushort)encodedLength,
                            Frequencies = frequencies,
                            UnitId = 100000,
                            Encryptions = encryptionBytes,
                            Modulations = modulationBytes,
                        };

                        udpVoiceHandler.Send(udpVoicePacket);
                    }
                   
                }
            }

        }

        public Task HandleAsync(TCPClientStatusMessage message, CancellationToken cancellationToken)
        {
            if (message.Connected)
            {
                ReadyToSend();
            }
            else
            {
                Disconnected();
            }

            return Task.CompletedTask;
        }

        private void Disconnected()
        {
            stop = true;

            StopEncoding();
        }

        public Task HandleAsync(PTTState message, CancellationToken cancellationToken)
        {
            PTTPressed = message.PTTPressed;

            return Task.CompletedTask;
        }
    }

   
    public class PTTState
    {
        public bool PTTPressed { get; set; }
    }
}
