using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using Newtonsoft.Json;
using NLog;
using LogManager = NLog.LogManager;

namespace Ciribob.SRS.Common.Network.Client;

public class TCPClientHandler : IHandle<DisconnectRequestMessage>, IHandle<UnitUpdateMessage>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    private static readonly int MAX_DECODE_ERRORS = 5;
    private readonly ConnectedClientsSingleton _clients = ConnectedClientsSingleton.Instance;
    private readonly string _guid;

    private readonly SyncedServerSettings _serverSettings = SyncedServerSettings.Instance;
    
    private long _lastSent = -1;
    private PlayerUnitStateBase _playerUnitState;
    private IPEndPoint _serverEndpoint;

    private volatile bool _stop;
    private TcpClient _tcpClient;

 //   private UDPVoiceHandler _udpVoiceHandler;

    private bool _connected = false;

    public bool TCPConnected
    {
        get
        {
            if (_tcpClient!=null)
            {
                return _tcpClient.Connected;
            }
            return false;
        }
    }

    public TCPClientHandler(string guid, PlayerUnitStateBase playerUnitState)
    {
        _clients.Clear();
        _guid = guid;
        _playerUnitState = playerUnitState;
    }

    public Task HandleAsync(DisconnectRequestMessage message, CancellationToken cancellationToken)
    {
        Disconnect();

        return Task.CompletedTask;
    }

    public Task HandleAsync(UnitUpdateMessage message, CancellationToken cancellationToken)
    {
        if (message.FullUpdate)
            ClientRadioUpdated(message.UnitUpdate);
        else
            ClientCoalitionUpdate(message.UnitUpdate);

        return Task.CompletedTask;
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

        var connectionError = false;

        using (_tcpClient = new TcpClient())
        {
            try
            {
                _tcpClient.SendTimeout = 90000;
                _tcpClient.NoDelay = true;

                // Wait for 10 seconds before aborting connection attempt - no SRS server running/port opened in that case
                _tcpClient.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port)
                    .Wait(TimeSpan.FromSeconds(10));

                if (_tcpClient.Connected)
                {
                    _tcpClient.NoDelay = true;

                    ClientSyncLoop(_playerUnitState);
                }
                else
                {
                    Logger.Error($"Failed to connect to server @ {_serverEndpoint}");
                    // Signal disconnect including an error
                    connectionError = true;
                    EventBus.Instance.PublishOnUIThreadAsync(new TCPClientStatusMessage(false, TCPClientStatusMessage.ErrorCode.TIMEOUT));

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not connect to server");
                connectionError = true;
                Disconnect();
            }
        }
    }

    private void ClientRadioUpdated(PlayerUnitStateBase updatedUnitState)
    {
        Logger.Debug("Sending Full Update to Server");

        updatedUnitState.LatLng = new LatLngPosition();
        //Only send if there is an actual change
        if (!updatedUnitState.Equals(_playerUnitState))
        {
            _playerUnitState = updatedUnitState;
            var message = new NetworkMessage
            {
                Client = new SRClientBase
                {
                    ClientGuid = _guid,
                    UnitState = updatedUnitState
                },
                MsgType = NetworkMessage.MessageType.FULL_UPDATE
            };

            SendToServer(message);
        }
    }

    private void ClientCoalitionUpdate(PlayerUnitStateBase updatedMetadata)
    {
        updatedMetadata.LatLng = new LatLngPosition();

        //only send if there is an actual change to metadata
        if (!_playerUnitState.MetaDataEquals(updatedMetadata))
        {
            _playerUnitState.UpdateMetadata(updatedMetadata);
            //dont send radios to cut down size
            updatedMetadata.Radios = new List<RadioBase>();

            var message = new NetworkMessage
            {
                Client = new SRClientBase
                {
                    ClientGuid = _guid,
                    UnitState = updatedMetadata
                },
                MsgType = NetworkMessage.MessageType.PARTIAL_UPDATE
            };

            //update state
            SendToServer(message);
        }
    }


    private void ClientSyncLoop(PlayerUnitStateBase initialState)
    {
        EventBus.Instance.SubscribeOnBackgroundThread(this);
        //clear the clients list
        _clients.Clear();
        var decodeErrors = 0; //if the JSON is unreadable - new version likely

        using (var reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8))
        {
            try
            {
                //TODO switch to proxy for everything
                //TODO remove _clientstate and just pass in the initial state
                //then use broadcasts / events for the rest

                //start the loop off by sending a SYNC Request
                SendToServer(new NetworkMessage
                {
                    Client = new SRClientBase
                    {
                        ClientGuid = _guid,
                        UnitState = initialState
                    },
                    MsgType = NetworkMessage.MessageType.SYNC
                });

                EventBus.Instance.PublishOnUIThreadAsync(new TCPClientStatusMessage(true, _serverEndpoint));

                string line;
                while ((line = reader.ReadLine()) != null)
                    try
                    {
                        var serverMessage = JsonConvert.DeserializeObject<NetworkMessage>(line);
                        decodeErrors = 0; //reset counter
                        if (serverMessage != null)
                            //Logger.Debug("Received "+serverMessage.MsgType);
                            switch (serverMessage.MsgType)
                            {
                                case NetworkMessage.MessageType.PING:
                                    // Do nothing for now
                                    break;
                                case NetworkMessage.MessageType.FULL_UPDATE:
                                case NetworkMessage.MessageType.PARTIAL_UPDATE:

                                    if (serverMessage.ServerSettings != null)
                                        _serverSettings.Decode(serverMessage.ServerSettings);

                                    SRClientBase srClient;
                                    if (_clients.TryGetValue(serverMessage.Client.ClientGuid, out srClient))
                                    {
                                        if (serverMessage.MsgType == NetworkMessage.MessageType.FULL_UPDATE)
                                            HandleFullUpdate(serverMessage, srClient);
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
                                    EventBus.Instance.PublishOnUIThreadAsync(new SRClientUpdateMessage(srClient));

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

                                    SyncedServerSettings.Instance.ServerVersion = serverMessage.Version;

                                    if (serverVersion < protocolVersion)
                                    {
                                        Logger.Error(
                                            $"Server version ({serverMessage.Version}) older than minimum procotol version ({UpdaterChecker.MINIMUM_PROTOCOL_VERSION}) - disconnecting");

                                        ShowVersionMistmatchWarning(serverMessage.Version);

                                        Disconnect();
                                        break;
                                    }

                                    if (serverMessage.Clients != null)
                                        foreach (var client in serverMessage.Clients)
                                        {
                                            client.LastUpdate = DateTime.Now.Ticks;
                                            //init with LOS true so you can hear them incase of bad DCS install where
                                            //LOS isnt working
                                            client.LineOfSightLoss = 0.0f;
                                            //0.0 is NO LOSS therefore full Line of sight
                                            _clients[client.ClientGuid] = client;

                                            EventBus.Instance.PublishOnUIThreadAsync(
                                                new SRClientUpdateMessage(client));
                                        }

                                    //add presets
                                    if (serverMessage.PresetChannels != null)
                                    {
                                        _serverSettings.SetPresetChannels(serverMessage.PresetChannels);
                                    }
                                    
                                    //add server settings
                                    _serverSettings.Decode(serverMessage.ServerSettings);
                                    break;

                                case NetworkMessage.MessageType.SERVER_SETTINGS:

                                    _serverSettings.Decode(serverMessage.ServerSettings);
                                    SyncedServerSettings.Instance.ServerVersion = serverMessage.Version;

                                    break;
                                case NetworkMessage.MessageType.CLIENT_DISCONNECT:

                                    SRClientBase outClient;
                                    _clients.TryRemove(serverMessage.Client.ClientGuid, out outClient);

                                    if (outClient != null)
                                        EventBus.Instance.PublishOnUIThreadAsync(
                                            new SRClientUpdateMessage(outClient, false));

                                    break;
                                case NetworkMessage.MessageType.VERSION_MISMATCH:
                                    Logger.Error(
                                        $"Version Mismatch Between Client ({UpdaterChecker.VERSION}) & Server ({serverMessage.Version}) - Disconnecting");

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
                    catch (Exception ex)
                    {
                        decodeErrors++;
                        if (!_stop) Logger.Error(ex, "Client exception reading from socket ");

                        if (decodeErrors > MAX_DECODE_ERRORS)
                        {
                            ShowVersionMistmatchWarning("unknown");
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

        //clear the clients list
        _clients.Clear();

        Disconnect();
    }

    private void HandlePartialUpdate(NetworkMessage networkMessage, SRClientBase client)
    {
        client.UnitState.Transponder = networkMessage.Client.UnitState.Transponder;
        client.UnitState.Coalition = networkMessage.Client.UnitState.Coalition;
        client.UnitState.LatLng = networkMessage.Client.UnitState.LatLng;
        client.UnitState.Name = networkMessage.Client.UnitState.Name;
        client.UnitState.UnitId = networkMessage.Client.UnitState.UnitId;
    }

    private void HandleFullUpdate(NetworkMessage networkMessage, SRClientBase client)
    {
        var updatedSrClient = networkMessage.Client;
        client.UnitState = updatedSrClient.UnitState;
    }

    private void ShowVersionMistmatchWarning(string serverVersion)
    {
        /*
        MessageBox.Show($"The SRS server you're connecting to is incompatible with this Client. " +
                        $"\n\nMake sure to always run the latest version of the SRS Server & Client" +
                        $"\nClient Version: {UpdaterChecker.VERSION}",
                        "SRS Server Incompatible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
        */
    }

    private void SendToServer(NetworkMessage message)
    {
        try
        {
            _lastSent = DateTime.Now.Ticks;
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
        EventBus.Instance.Unsubcribe(this);

        _stop = true;

        _lastSent = DateTime.Now.Ticks;

        try
        {
            if (_tcpClient != null)
            {
                _tcpClient?.Close(); // this'll stop the socket blocking
                _tcpClient = null;
                EventBus.Instance.PublishOnUIThreadAsync(new TCPClientStatusMessage(false));
            }
        }
        catch (Exception ex)
        {
        }
        //
        // try
        // {
        //     _udpVoiceHandler?.RequestStop(); // this'll stop the socket blocking
        //     _udpVoiceHandler = null;
        // }
        // catch (Exception ex)
        // {
        // }

        Logger.Error("Disconnecting from server");
    }
}