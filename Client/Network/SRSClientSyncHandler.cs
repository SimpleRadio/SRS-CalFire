using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.SRS.Common.Network;
using Ciribob.SRS.Common.Setting;
using Ciribob.SRS.Client.Network.Sync;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.PlayerState;
using Easy.MessageHub;
using Newtonsoft.Json;
using NLog;

namespace Ciribob.SRS.Client.Network
{
    public class SRSClientSyncHandler
    {
        public delegate void ConnectCallback(bool result, bool connectionError, string connection);
        public delegate void UpdateUICallback();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private volatile bool _stop = false;

        public static string ServerVersion = "Unknown";
        private readonly string _guid;
        private ConnectCallback _callback;
        private UpdateUICallback _updateUICallback;
        private readonly RadioSyncHandler.NewAircraft _newAircraft;
        private IPEndPoint _serverEndpoint;
        private TcpClient _tcpClient;

        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;
        private readonly SyncedServerSettings _serverSettings = SyncedServerSettings.Instance;
        private readonly ConnectedClientsSingleton _clients = ConnectedClientsSingleton.Instance;


        private RadioSyncManager _radioSync = null;

        private static readonly int MAX_DECODE_ERRORS = 5;

        private long _lastSent = -1;


        public SRSClientSyncHandler(string guid, UpdateUICallback uiCallback, RadioSyncHandler.NewAircraft _newAircraft)
        {
            _guid = guid;
            _updateUICallback = uiCallback;
            this._newAircraft = _newAircraft;

          
        }

  


        public void TryConnect(IPEndPoint endpoint, ConnectCallback callback)
        {
            _callback = callback;
            _serverEndpoint = endpoint;

            var tcpThread = new Thread(Connect);
            tcpThread.Start();
        }


        private void Connect()
        {
            _lastSent = DateTime.Now.Ticks;

            if (_radioSync != null)
            {
                _radioSync.Stop();
                _radioSync = null;
            }
          

            bool connectionError = false;

            _radioSync = new RadioSyncManager(ClientRadioUpdated, ClientCoalitionUpdate, _guid,_newAircraft);

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
                        _radioSync.Start();

                        _tcpClient.NoDelay = true;

                        CallOnMain(true);
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

            _radioSync.Stop();

            //disconnect callback
            CallOnMain(false, connectionError);
        }

        private void ClientRadioUpdated()
        {
           
            Logger.Debug("Sending Radio Update to Server");
            var sideInfo = _clientStateSingleton.PlayerCoaltionLocationMetadata;

            var message = new NetworkMessage
            {
                Client = new SRClient
                {
                    Name = sideInfo.name,
                    ClientGuid = _guid,
                    UnitState = _clientStateSingleton.PlayerUnitState
                },
                MsgType = NetworkMessage.MessageType.FULL_UPDATE
            };

            var needValidPosition = _serverSettings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED) || _serverSettings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED);

            if (needValidPosition)
            {
                message.Client.LatLngPosition = sideInfo.LngLngPosition;
            }
            else
            {
                message.Client.LatLngPosition = new LatLngPosition();
            }

            SendToServer(message);

           
        }

        private void ClientCoalitionUpdate()
        {
            var sideInfo = _clientStateSingleton.PlayerCoaltionLocationMetadata;

            var message =  new NetworkMessage
            {
                Client = new SRClient
                {
                    Name = sideInfo.name,
                    ClientGuid = _guid
                },
                MsgType = NetworkMessage.MessageType.UPDATE
            };

            var needValidPosition = _serverSettings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED) || _serverSettings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED);

            if (needValidPosition)
            {
                message.Client.LatLngPosition = sideInfo.LngLngPosition;
            }
            else
            {
                message.Client.LatLngPosition = new LatLngPosition();
            }

            SendToServer(message);
        }

        private void CallOnMain(bool result, bool connectionError = false)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                    new ThreadStart(delegate { _callback(result, connectionError, _serverEndpoint.ToString()); }));
            }
            catch (Exception ex)
            {
                 Logger.Error(ex, "Failed to update UI after connection callback (result {result}, connectionError {connectionError})", result, connectionError);
            }
        }

   

        private void CallUpdateUIOnMain()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                    new ThreadStart(delegate { _updateUICallback(); }));
            }
            catch (Exception ex)
            {
            }
        }

        private void ClientSyncLoop()
        {
            //clear the clients list
            _clients.Clear();
            int decodeErrors = 0; //if the JSON is unreadable - new version likely

            using (var reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8))
            {
                try
                {
                    var sideInfo = _clientStateSingleton.PlayerCoaltionLocationMetadata;
                    //start the loop off by sending a SYNC Request
                    SendToServer(new NetworkMessage
                    {
                        Client = new SRClient
                        {
                            Name = sideInfo.name.Length > 0 ? sideInfo.name : _clientStateSingleton.LastSeenName,
                            LatLngPosition = sideInfo.LngLngPosition,
                            ClientGuid = _guid,
                            UnitState = _clientStateSingleton.PlayerUnitState
                        },
                        MsgType = NetworkMessage.MessageType.SYNC,
                    });

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
                                    case NetworkMessage.MessageType.UPDATE:

                                        if (serverMessage.ServerSettings != null)
                                        {
                                            _serverSettings.Decode(serverMessage.ServerSettings);
                                        }
                                        
                                        if (_clients.ContainsKey(serverMessage.Client.ClientGuid))
                                        {
                                            var srClient = _clients[serverMessage.Client.ClientGuid];
                                            var updatedSrClient = serverMessage.Client;
                                            if (srClient != null)
                                            {
                                                srClient.LastUpdate = DateTime.Now.Ticks;
                                                srClient.Name = updatedSrClient.Name;

                                                srClient.LatLngPosition = updatedSrClient.LatLngPosition;

                                                if (updatedSrClient.UnitState != null)
                                                {
                                                    srClient.UnitState = updatedSrClient.UnitState;
                                                    srClient.UnitState.LastUpdate = DateTime.Now.Ticks;
                                                }
                                                else
                                                {
                                                    //radio update but null RadioInfo means no change
                                                    if (serverMessage.MsgType ==
                                                        NetworkMessage.MessageType.FULL_UPDATE &&
                                                        srClient.UnitState != null)
                                                    {
                                                        srClient.UnitState.LastUpdate = DateTime.Now.Ticks;
                                                    }
                                                }

                                                // Logger.Debug("Received Update Client: " + NetworkMessage.MessageType.UPDATE + " From: " +
                                                //             srClient.Name + " Coalition: " +
                                                //             srClient.Coalition + " Pos: " + srClient.LatLngPosition);
                                            }
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

                                            // Logger.Debug("Received New Client: " + NetworkMessage.MessageType.UPDATE +
                                            //             " From: " +
                                            //             serverMessage.Client.Name + " Coalition: " +
                                            //             serverMessage.Client.Coalition);
                                        }


                                        CallUpdateUIOnMain();

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
                                            }
                                        }
                                        //add server settings
                                        _serverSettings.Decode(serverMessage.ServerSettings);


                                        CallUpdateUIOnMain();

                                        break;

                                    case NetworkMessage.MessageType.SERVER_SETTINGS:

                                        _serverSettings.Decode(serverMessage.ServerSettings);
                                        ServerVersion = serverMessage.Version;


                                        CallUpdateUIOnMain();

                                        break;
                                    case NetworkMessage.MessageType.CLIENT_DISCONNECT:

                                        SRClient outClient;
                                        _clients.TryRemove(serverMessage.Client.ClientGuid, out outClient);

                                        if (outClient != null)
                                        {
                                            MessageHub.Instance.Publish(outClient);
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

        private void ShowVersionMistmatchWarning(string serverVersion)
        {
            MessageBox.Show($"The SRS server you're connecting to is incompatible with this Client. " +
                            $"\n\nMake sure to always run the latest version of the SRS Server & Client" +
                            $"\n\nServer Version: {serverVersion}" +
                            $"\nClient Version: {UpdaterChecker.VERSION}",
                            "SRS Server Incompatible",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
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
            _stop = true;

            _lastSent = DateTime.Now.Ticks;

            try
            {
                if (_tcpClient != null)
                {
                    _tcpClient.Close(); // this'll stop the socket blocking
                }
            }
            catch (Exception ex)
            {
            }

            Logger.Error("Disconnecting from server");
            ClientStateSingleton.Instance.IsConnected = false;

            //CallOnMain(false);
        }
    }
}