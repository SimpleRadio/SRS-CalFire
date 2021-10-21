using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using Ciribob.SRS.Common.PlayerState;
using Ciribob.SRS.Common.Setting;
using Easy.MessageHub;
using NLog;

namespace Ciribob.SRS.Common.Network.Client
{
    public class UDPVoiceHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public BlockingCollection<byte[]> EncodedAudio { get; } = new BlockingCollection<byte[]>();
        private readonly byte[] _guidAsciiBytes;
        private readonly CancellationTokenSource _pingStop = new CancellationTokenSource();

        private const int UDP_VOIP_TIMEOUT = 42; // seconds for timeout before redoing VoIP

        private UdpClient _listener;

        private ulong _packetNumber = 1;

        public bool Ready { get; private set; }
        private readonly IPEndPoint _serverEndpoint;
        private volatile bool _stop;
        private long _udpLastReceived = 0;
        private readonly DispatcherTimer _updateTimer;

        public delegate void ConnectionState(bool voipConnected);

        private readonly ConnectedClientsSingleton _clients = ConnectedClientsSingleton.Instance;
        private readonly MessageHub _hubSingleton = MessageHubSingleton.Instance;

        public ConnectionState ConnectionStateDelegate { get; }

        public UDPVoiceHandler(string guid, IPEndPoint endPoint)
        {
            _guidAsciiBytes = Encoding.ASCII.GetBytes(guid);

            _serverEndpoint = endPoint;

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _updateTimer.Tick += UpdateVOIPStatus;
            _updateTimer.Start();
        }

        private void UpdateVOIPStatus(object sender, EventArgs e)
        {
            var diff = TimeSpan.FromTicks(DateTime.Now.Ticks - _udpLastReceived);

            //ping every 10 so after 40 seconds VoIP UDP issue
            if (diff.TotalSeconds > UDP_VOIP_TIMEOUT)
            {
                //TODO emit event / callback

                ConnectionStateDelegate?.Invoke(false);
                _hubSingleton.Publish(new VOIPStatusMessage(false));
            }
            else
            {
                ConnectionStateDelegate?.Invoke(true);
                _hubSingleton.Publish(new VOIPStatusMessage(true));
            }
        }


        public void Listen()
        {
            _udpLastReceived = 0;
            Ready = false;
            _listener = new UdpClient();
            try
            {
                _listener.AllowNatTraversal(true);
            }
            catch
            {
            }

            StartPing();

            _packetNumber = 1; //reset packet number

            while (!_stop)
                if (Ready)
                    try
                    {
                        var groupEp = new IPEndPoint(IPAddress.Any, _serverEndpoint.Port);

                        var bytes = _listener.Receive(ref groupEp);

                        if (bytes?.Length == 22)
                        {
                            _udpLastReceived = DateTime.Now.Ticks;
                            Logger.Info("Received Ping Back from Server");
                        }
                        else if (bytes?.Length > 22)
                        {
                            _udpLastReceived = DateTime.Now.Ticks;
                            EncodedAudio.Add(bytes);
                        }
                    }
                    catch (Exception e)
                    {
                        //  logger.Error(e, "error listening for UDP Voip");
                    }

            Ready = false;

            //stop UI Refreshing
            _updateTimer.Stop();

            //TODO send event & callback
            ConnectionStateDelegate?.Invoke(false);
            _hubSingleton.Publish(new VOIPStatusMessage(false));
        }


        public void RequestStop()
        {
            _stop = true;
            try
            {
                _listener?.Close();
                _listener = null;
            }
            catch (Exception e)
            {
            }

            try
            {
                _pingStop.Cancel();
            }
            catch (Exception ex)
            {
            }

            ConnectionStateDelegate?.Invoke(false);
            _hubSingleton.Publish(new VOIPStatusMessage(false));
        }

        public bool Send(UDPVoicePacket udpVoicePacket)
        {
            if (Ready
                && _listener != null
                && udpVoicePacket != null)
                try
                {
                    //TODO check this
                    udpVoicePacket.PacketNumber++;
                    var encodedUdpVoicePacket = udpVoicePacket.EncodePacket();

                    _listener.Send(encodedUdpVoicePacket, encodedUdpVoicePacket.Length, _serverEndpoint);

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Exception Sending Audio Message " + e.Message);
                }


            return false;
        }

        private void StartPing()
        {
            Logger.Info("Pinging Server - Starting");

            var message = _guidAsciiBytes;

            // Force immediate ping once to avoid race condition before starting to listen
            _listener?.Send(message, message.Length, _serverEndpoint);

            var thread = new Thread(() =>
            {
                //wait for initial sync - then ping
                if (_pingStop.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(2))) return;

                Ready = true;

                while (!_stop)
                {
                    //Logger.Info("Pinging Server");
                    try
                    {
                        _listener?.Send(message, message.Length, _serverEndpoint);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Exception Sending Audio Ping! " + e.Message);
                    }

                    //wait for cancel or quit
                    var cancelled = _pingStop.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));

                    if (cancelled) return;

                    var diff = TimeSpan.FromTicks(DateTime.Now.Ticks - _udpLastReceived);

                    //reconnect to UDP - port is no good!
                    if (diff.TotalSeconds > UDP_VOIP_TIMEOUT)
                    {
                        Logger.Error("VoIP Timeout - Recreating VoIP Connection");
                        Ready = false;
                        try
                        {
                            _listener?.Close();
                        }
                        catch (Exception ex)
                        {
                        }

                        _listener = null;

                        _udpLastReceived = 0;

                        _listener = new UdpClient();
                        try
                        {
                            _listener.AllowNatTraversal(true);
                        }
                        catch
                        {
                        }

                        try
                        {
                            // Force immediate ping once to avoid race condition before starting to listen
                            _listener.Send(message, message.Length, _serverEndpoint);
                            Ready = true;
                            Logger.Error("VoIP Timeout - Success Recreating VoIP Connection");
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Exception Sending Audio Ping! " + e.Message);
                        }
                    }
                }
            });
            thread.Start();
        }
    }
}