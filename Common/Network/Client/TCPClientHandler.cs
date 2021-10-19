using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using Ciribob.SRS.Common.PlayerState;
using Ciribob.SRS.Common.Setting;
using Newtonsoft.Json;
using NLog;

namespace Ciribob.SRS.Common.Network.Client
{
    public class TCPClientHandler
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private volatile bool _stop = false;

        public static string ServerVersion = "Unknown";
        private readonly string _guid;
        private IPEndPoint _serverEndpoint;
        private TcpClient _tcpClient;

        private readonly SyncedServerSettings _serverSettings = SyncedServerSettings.Instance;
        private readonly ConnectedClientsSingleton _clients = ConnectedClientsSingleton.Instance;

        private static readonly int MAX_DECODE_ERRORS = 5;

        private long _lastSent = -1;
        private readonly List<Guid> _subscriptions = new List<Guid>();

        private UdpVoiceHandler _udpVoiceHandler;

        private ClientStateSingleton _clientState = ClientStateSingleton.Instance;


        public TCPClientHandler()
        {
            _clients.Clear();
            this._guid = ClientStateSingleton.Instance.GUID;
        }

        public void TryConnect(IPEndPoint endpoint)
        {
            _serverEndpoint = endpoint;

            var tcpThread = new Thread(Connect);
            tcpThread.Start();
        }


        private void Connect()
        {
            _lastSent = DateTime.Now.Ticks;

            bool connectionError = false;

            using (_tcpClient = new TcpClient())
            {
                try
                {
                    _tcpClient.SendTimeout = 90000;
                    _tcpClient.NoDelay = true;

                    // Wait for 10 seconds before aborting connection attempt - no SRS server running/port opened in that case
                    _tcpClient.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).Wait(TimeSpan.FromSeconds(10));

                    if (_tcpClient.Connected)
                    {
                       
                        _tcpClient.NoDelay = true;

                        ClientSyncLoop();
                    }
                    else
                    {
                        Logger.Error($"Failed to connect to server @ {_serverEndpoint.ToString()}");

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

            //_radioSync.Stop();


            //disconnect callback
            MessageHubSingleton.Instance.Publish(new TCPClientStatusMessage(false));
        }

        private void ClientRadioUpdated(PlayerUnitState state)
        {
           
            Logger.Debug("Sending Full Update to Server");

            var message = new NetworkMessage
            {
                Client = new SRClient
                {
                    ClientGuid = _guid,
                    UnitState = state
                },
                MsgType = NetworkMessage.MessageType.FULL_UPDATE
            };

            var needValidPosition = _serverSettings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED) || _serverSettings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED);
            //TODO come back too
            if (!needValidPosition)
            {
                message.Client.UnitState.LatLng = new LatLngPosition();
            }

            SendToServer(message);

           
        }

        private void ClientCoalitionUpdate(PlayerUnitState state)
        {
         
            var message =  new NetworkMessage
            {
                Client = new SRClient
                {
                    ClientGuid = _guid,
                    UnitState = state
                },
                MsgType = NetworkMessage.MessageType.PARTIAL_UPDATE
            };

            var needValidPosition = _serverSettings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED) || _serverSettings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED);
            //TODO come back too
            if (!needValidPosition)
            {
                message.Client.UnitState.LatLng = new LatLngPosition();
            }

            SendToServer(message);
        }

    
        private void ClientSyncLoop()
        {
            //subscribe to disconnect
            _subscriptions.Add(MessageHubSingleton.Instance.Subscribe<DisconnectRequestMessage>(msg => Disconnect()));

            //clear the clients list
            _clients.Clear();
            int decodeErrors = 0; //if the JSON is unreadable - new version likely

            using (var reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8))
            {
                try
                {
                    var sideInfo = _clientState.PlayerUnitState;
                    //start the loop off by sending a SYNC Request
                    SendToServer(new NetworkMessage
                    {
                        Client = new SRClient
                        {
                            ClientGuid = _guid,
                            UnitState = sideInfo
                        },
                        MsgType = NetworkMessage.MessageType.SYNC,
                    });

                    var udpHandler = new UdpVoiceHandler(_guid, _serverEndpoint);

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        try
                        {
                            var serverMessage = JsonConvert.DeserializeObject<NetworkMessage>(line);
                            decodeErrors = 0; //reset counter
                            if (serverMessage != null)
                            {
                                //Logger.Debug("Received "+serverMessage.MsgType);
                                switch (serverMessage.MsgType)
                                {
                                    case NetworkMessage.MessageType.PING:
                                        // Do nothing for now
                                        break;
                                    case NetworkMessage.MessageType.FULL_UPDATE:
                                    case NetworkMessage.MessageType.PARTIAL_UPDATE:

                                        if (serverMessage.ServerSettings != null)
                                        {
                                            _serverSettings.Decode(serverMessage.ServerSettings);
                                        }

                                        SRClient srClient;
                                        if (_clients.TryGetValue(serverMessage.Client.ClientGuid, out srClient))
                                        {
                                            if(serverMessage.MsgType == NetworkMessage.MessageType.FULL_UPDATE)
                                                HandleFullUpdate(serverMessage,srClient);
                                            else if (serverMessage.MsgType == NetworkMessage.MessageType.PARTIAL_UPDATE)
                                                HandlePartialUpdate(serverMessage, srClient);
                                        }
                                        else
                                        {
                                            
                                            var connectedClient = serverMessage.Client;
                                            connectedClient.LastUpdate = DateTime.Now.Ticks;

                                            //init with LOS true so you can hear them incase of bad DCS install where
                                            //LOS isnt working
                                            connectedClient.LineOfSightLoss = 0.0f;
                                            //0.0 is NO LOSS therefore full Line of sight

                                            _clients[serverMessage.Client.ClientGuid] = connectedClient;

                                            srClient = connectedClient;

                                            // Logger.Debug("Received New Client: " + NetworkMessage.MessageType.UPDATE +
                                            //             " From: " +
                                            //             serverMessage.Client.Name + " Coalition: " +
                                            //             serverMessage.Client.Coalition);

                                        }

                                        srClient.LastUpdate = DateTime.Now.Ticks;
                                        MessageHubSingleton.Instance.Publish(new SRClientUpdateMessage(srClient));

                                        break;
                                    case NetworkMessage.MessageType.SYNC:
                                        // Logger.Info("Recevied: " + NetworkMessage.MessageType.SYNC);

                                        //check server version
                                        if (serverMessage.Version == null)
                                        {
                                            Logger.Error("Disconnecting Unversioned Server");
                                            Disconnect();
                                            break;
                                        }

                                        var serverVersion = Version.Parse(serverMessage.Version);
                                        var protocolVersion = Version.Parse(UpdaterChecker.MINIMUM_PROTOCOL_VERSION);

                                        ServerVersion = serverMessage.Version;

                                        if (serverVersion < protocolVersion)
                                        {
                                            Logger.Error($"Server version ({serverMessage.Version}) older than minimum procotol version ({UpdaterChecker.MINIMUM_PROTOCOL_VERSION}) - disconnecting");

                                            ShowVersionMistmatchWarning(serverMessage.Version);

                                            Disconnect();
                                            break;
                                        }

                                        if (serverMessage.Clients != null)
                                        {
                                            foreach (var client in serverMessage.Clients)
                                            {
                                                client.LastUpdate = DateTime.Now.Ticks;
                                                //init with LOS true so you can hear them incase of bad DCS install where
                                                //LOS isnt working
                                                client.LineOfSightLoss = 0.0f;
                                                //0.0 is NO LOSS therefore full Line of sight
                                                _clients[client.ClientGuid] = client;

                                                MessageHubSingleton.Instance.Publish(new SRClientUpdateMessage(client));
                                            }
                                        }
                                        //add server settings
                                        _serverSettings.Decode(serverMessage.ServerSettings);

                                        break;

                                    case NetworkMessage.MessageType.SERVER_SETTINGS:

                                        _serverSettings.Decode(serverMessage.ServerSettings);
                                        ServerVersion = serverMessage.Version;

                                        break;
                                    case NetworkMessage.MessageType.CLIENT_DISCONNECT:

                                        SRClient outClient;
                                        _clients.TryRemove(serverMessage.Client.ClientGuid, out outClient);

                                        if (outClient != null)
                                        {
                                            MessageHubSingleton.Instance.Publish(new SRClientUpdateMessage(outClient,false));
                                        }

                                        break;
                                    case NetworkMessage.MessageType.VERSION_MISMATCH:
                                        Logger.Error($"Version Mismatch Between Client ({UpdaterChecker.VERSION}) & Server ({serverMessage.Version}) - Disconnecting");

                                        ShowVersionMistmatchWarning(serverMessage.Version);

                                        Disconnect();
                                        break;
                                    case NetworkMessage.MessageType.EXTERNAL_AWACS_MODE_PASSWORD:
                                       
                                        break;
                                    default:
                                        Logger.Error("Recevied unknown " + line);
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            decodeErrors++;
                            if (!_stop)
                            {
                                Logger.Error(ex, "Client exception reading from socket ");
                            }

                            if (decodeErrors > MAX_DECODE_ERRORS)
                            {
                                ShowVersionMistmatchWarning("unknown");
                                Disconnect();
                                break;
                            }
                        }

                        // do something with line
                    }
                }
                catch (Exception ex)
                {
                    if (!_stop)
                    {
                        Logger.Error(ex, "Client exception reading - Disconnecting ");
                    }
                }
            }

            //disconnected - reset DCS Info
            ClientStateSingleton.Instance.PlayerUnitState.LastUpdate = 0;

            //clear the clients list
            _clients.Clear();

            Disconnect();
        }

        private void HandlePartialUpdate(NetworkMessage networkMessage, SRClient client)
        {
            var updatedSrClient = networkMessage.Client;
            //TODO change to internal proxy
            client.UnitState.Transponder = client.UnitState.Transponder;
            client.UnitState.Coalition = client.UnitState.Coalition;
            client.UnitState.LatLng = client.UnitState.LatLng;
            client.UnitState.Name = client.UnitState.Name;

        }

        private void HandleFullUpdate(NetworkMessage networkMessage, SRClient client)
        {
            var updatedSrClient = networkMessage.Client;
            //TODO change to internal proxy
            client.UnitState = updatedSrClient.UnitState;

        }

        private void ShowVersionMistmatchWarning(string serverVersion)
        {
            //TODO send message to alert about this
            // MessageBox.Show($"The SRS server you're connecting to is incompatible with this Client. " +
            //                 $"\n\nMake sure to always run the latest version of the SRS Server & Client" +
            //                 $"\n\nServer Version: {serverVersion}" +
            //                 $"\nClient Version: {UpdaterChecker.VERSION}",
            //                 "SRS Server Incompatible",
            //                 MessageBoxButton.OK,
            //                 MessageBoxImage.Error);
        }

        private void SendToServer(NetworkMessage message)
        {
            try
            {
                _lastSent = DateTime.Now.Ticks;
                message.Version = UpdaterChecker.VERSION;

                var json = message.Encode();

                if (message.MsgType == NetworkMessage.MessageType.FULL_UPDATE)
                {
                    Logger.Debug("Sending Radio Update To Server: "+ (json));
                }

                var bytes = Encoding.UTF8.GetBytes(json);
                _tcpClient.GetStream().Write(bytes, 0, bytes.Length);
                //Need to flush?
            }
            catch (Exception ex)
            {
                if (!_stop)
                {
                    Logger.Error(ex, "Client exception sending to server");
                }

                Disconnect();
            }
        }

        //implement IDispose? To close stuff properly?
        public void Disconnect()
        {
            foreach (var token in _subscriptions)
            {
                MessageHubSingleton.Instance.Unsubscribe(token);
            }
          
            _stop = true;

            _lastSent = DateTime.Now.Ticks;

            try
            {
                _tcpClient?.Close(); // this'll stop the socket blocking
                _tcpClient = null;
            }
            catch (Exception ex)
            {
            }

            try
            {
                _udpVoiceHandler?.RequestStop(); // this'll stop the socket blocking
                _udpVoiceHandler = null;
            }
            catch (Exception ex)
            {
            }

            Logger.Error("Disconnecting from server");

            MessageHubSingleton.Instance.Publish(new TCPClientStatusMessage(false));

            //CallOnMain(false);
        }
    }
}