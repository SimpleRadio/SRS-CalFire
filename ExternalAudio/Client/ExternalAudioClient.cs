using System;
using System.Net;
using System.Threading;
using Ciribob.FS3D.SimpleRadio.Standalone.ExternalAudioClient.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.ExternalAudioClient.Models;
using Ciribob.FS3D.SimpleRadio.Standalone.ExternalAudioClient.Network;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Singletons;
using NLog;
using Timer = Ciribob.SRS.Common.Timers.Timer;

namespace Ciribob.FS3D.SimpleRadio.Standalone.ExternalAudioClient.Client
{
    internal class ExternalAudioClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string Guid = ShortGuid.NewGuid();

        private readonly CancellationTokenSource finished = new();

        private readonly double[] freq;
        private PlayerUnitState gameState;
        private readonly Modulation[] modulation;
        private readonly byte[] modulationBytes;
        private readonly Program.Options opts;
        private UdpVoiceHandler udpVoiceHandler;

        public ExternalAudioClient(double[] freq, Modulation[] modulation, Program.Options opts)
        {
            this.freq = freq;
            this.modulation = modulation;
            this.opts = opts;
            modulationBytes = new byte[modulation.Length];
            for (var i = 0; i < modulationBytes.Length; i++) modulationBytes[i] = (byte)modulation[i];
        }

        public void Start()
        {
            EventBus.Instance.Subscribe<ReadyMessage>(ReadyToSend);
            EventBus.Instance.Subscribe<DisconnectedMessage>(Disconnected);

            gameState = new PlayerUnitState();
            gameState.Radios[1].Modulation = modulation[0];
            gameState.Radios[1].Freq = freq[0]; // get into Hz
            gameState.Radios[1].Name = opts.Name;

            Logger.Info("Starting with params:");
            for (var i = 0; i < freq.Length; i++) Logger.Info($"Frequency: {freq[i]} Hz - {modulation[i]} ");

            var position = new LatLngPosition
            {
                Alt = opts.Altitude,
                Lat = opts.Latitude,
                Lng = opts.Longitude
            };

            var srsClientSyncHandler = new SRSClientSyncHandler(Guid, gameState, opts.Name, opts.Coalition, position);

            srsClientSyncHandler.TryConnect(new IPEndPoint(IPAddress.Loopback, opts.Port));

            //wait for it to end
            finished.Token.WaitHandle.WaitOne();
            Logger.Info("Finished - Closing");

            udpVoiceHandler?.RequestStop();
            srsClientSyncHandler?.Disconnect();

            EventBus.Instance.ClearSubscriptions();
        }

        private void ReadyToSend(ReadyMessage ready)
        {
            if (udpVoiceHandler == null)
            {
                Logger.Info("Connecting UDP VoIP");
                udpVoiceHandler = new UdpVoiceHandler(Guid, IPAddress.Loopback, opts.Port, gameState);
                udpVoiceHandler.Start();
                new Thread(SendAudio).Start();
            }
        }

        private void Disconnected(DisconnectedMessage disconnected)
        {
            finished.Cancel();
        }

        private void SendAudio()
        {
            Logger.Info("Sending Audio... Please Wait");
            var audioGenerator = new AudioGenerator(opts);
            var opusBytes = audioGenerator.GetOpusBytes();
            var count = 0;

            var tokenSource = new CancellationTokenSource();

            //get all the audio as Opus frames of 40 ms
            //send on 40 ms timer 

            //when empty - disconnect
            //user timer for accurate sending
            var _timer = new Timer(() =>
            {
                if (!finished.IsCancellationRequested)
                {
                    if (count < opusBytes.Count)
                    {
                        udpVoiceHandler.Send(opusBytes[count], opusBytes[count].Length, freq, modulationBytes);
                        count++;

                        if (count % 50 == 0)
                            Logger.Info(
                                $"Playing audio - sent {count * 40}ms - {count / (float)opusBytes.Count * 100.0:F0}% ");
                    }
                    else
                    {
                        tokenSource.Cancel();
                    }
                }
                else
                {
                    Logger.Error("Client Disconnected");
                    tokenSource.Cancel();
                }
            }, TimeSpan.FromMilliseconds(40));
            _timer.Start();

            //wait for cancel
            tokenSource.Token.WaitHandle.WaitOne();
            _timer.Stop();

            Logger.Info("Finished Sending Audio");
            finished.Cancel();
        }
    }
}