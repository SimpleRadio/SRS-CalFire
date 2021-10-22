using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Proxies;
using Ciribob.SRS.Common.PlayerState;
using Ciribob.SRS.Common.Setting;
using Newtonsoft.Json;
using NLog;

namespace Ciribob.FS3D.SimpleRadio.Standalone.ExternalAudioClient.Network
{
    public class SRSClientSyncHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly int MAX_DECODE_ERRORS = 5;

        private readonly string _guid;
        private readonly PlayerUnitStateBase gameState;
        private IPEndPoint _serverEndpoint;

        private volatile bool _stop;
        private TcpClient _tcpClient;


        public SRSClientSyncHandler(string guid, PlayerUnitStateBase gameState, string name, int coalition,
            LatLngPosition position)
        {
            _guid = guid;
            this.gameState = gameState;

            this.gameState.Name = name;
            this.gameState.LatLng = position;
            this.gameState.Coalition = coalition;
        }

        public void TryConnect(IPEndPoint endpoint)
        {
            _serverEndpoint = endpoint;
            new Thread(Connect).Start();
        }

        private void Connect()
        {
            var connectionError = false;

            using (_tcpClient = new TcpClient())
            {
                try
                {
                    Logger.Info($"Connecting to server @{_serverEndpoint.Address}:{_serverEndpoint.Port} ");
                    _tcpClient.SendTimeout = 90000;
                    _tcpClient.NoDelay = true;

                    // Wait for 10 seconds before aborting connection attempt - no SRS server running/port opened in that case
                    _tcpClient.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port)
                        .Wait(TimeSpan.FromSeconds(10));

                    if (_tcpClient.Connected)
                    {
                        Logger.Info($"Connected to {_serverEndpoint.Address}:{_serverEndpoint.Port} ");
                        _tcpClient.NoDelay = true;
                        ClientSyncLoop();
                    }
                    else
                    {
                        Logger.Error($"Failed to connect to server @ {_serverEndpoint}");

                        // Signal disconnect including an error
                        connectionError = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not connect to server");
                    connectionError = true;
                }
            }


            //disconnect callback
            //TODO send disconnect
            // EventBus.Instance.Publish(new DisconnectedMessage());
        }

        private void ClientSyncLoop()
        {
            var decodeErrors = 0; //if the JSON is unreadable - new version likely

            using (var reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8))
            {
                try
                {
                    Logger.Info($"Sending client sync to {_serverEndpoint.Address}:{_serverEndpoint.Port} ");
                    //start the loop off by sending a SYNC Request
                    SendToServer(new NetworkMessage
                    {
                        Client = new SRClient
                        {
                            ClientGuid = _guid,
                            UnitState = gameState
                        },
                        MsgType = NetworkMessage.MessageType.SYNC
                    });

                    Logger.Info($"Sending radio update to {_serverEndpoint.Address}:{_serverEndpoint.Port} ");
                    SendToServer(new NetworkMessage
                    {
                        Client = new SRClient
                        {
                            ClientGuid = _guid,
                            UnitState = gameState
                        },
                        MsgType = NetworkMessage.MessageType.FULL_UPDATE
                    });

                    string line;
                    while ((line = reader.ReadLine()) != null)
                        try
                        {
                            Logger.Debug("Received update from Server: " + line);
                            var serverMessage = JsonConvert.DeserializeObject<NetworkMessage>(line);
                            decodeErrors = 0; //reset counter
                            if (serverMessage != null)
                                //Logger.Debug("Received "+serverMessage.MsgType);
                                switch (serverMessage.MsgType)
                                {
                                    case NetworkMessage.MessageType.PING:
                                    case NetworkMessage.MessageType.FULL_UPDATE:
                                    case NetworkMessage.MessageType.SERVER_SETTINGS:
                                    case NetworkMessage.MessageType.CLIENT_DISCONNECT:
                                        break;
                                    case NetworkMessage.MessageType.SYNC:
                                        // response to sync - kick off everything
                                        // EventBus.Instance.Publish(new ReadyMessage());

                                        break;
                                    case NetworkMessage.MessageType.VERSION_MISMATCH:
                                        Logger.Error(
                                            $"Version Mismatch Between Client ({UpdaterChecker.VERSION}) & Server ({serverMessage.Version}) - Disconnecting");

                                        Disconnect();
                                        break;
                                    default:
                                        Logger.Error("Received unknown Message " + line);
                                        break;
                                }
                        }
                        catch (Exception ex)
                        {
                            decodeErrors++;
                            if (!_stop) Logger.Error(ex, "Client exception reading from socket ");

                            if (decodeErrors > MAX_DECODE_ERRORS)
                            {
                                Disconnect();
                                break;
                            }
                        }

                    // do something with line
                }
                catch (Exception ex)
                {
                    if (!_stop) Logger.Error(ex, "Client exception reading - Disconnecting ");
                }
            }

            Disconnect();
        }

        private void SendToServer(NetworkMessage message)
        {
            try
            {
                message.Version = UpdaterChecker.VERSION;

                var json = message.Encode();

                if (message.MsgType == NetworkMessage.MessageType.FULL_UPDATE)
                    Logger.Debug("Sending Radio Update To Server: " + json);

                var bytes = Encoding.UTF8.GetBytes(json);
                _tcpClient.GetStream().Write(bytes, 0, bytes.Length);
                //Need to flush?
            }
            catch (Exception ex)
            {
                if (!_stop) Logger.Error(ex, "Client exception sending to server");

                Disconnect();
            }
        }

        //implement IDispose? To close stuff properly?
        public void Disconnect()
        {
            _stop = true;

            try
            {
                if (_tcpClient != null)
                    _tcpClient.Close(); // this'll stop the socket blocking

                //  EventBus.Instance.Publish(new DisconnectedMessage());
            }
            catch (Exception ex)
            {
                //   EventBus.Instance.Publish(new DisconnectedMessage());
            }

            Logger.Info("Disconnecting from server");
        }
    }
}