using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Network.Models;
using Ciribob.SRS.Common.Network.Models;
using NLog;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.Network
{
    public class ServerState : IHandle<StartServerMessage>, IHandle<StopServerMessage>, IHandle<KickClientMessage>,
        IHandle<BanClientMessage>
    {
        private static readonly string DEFAULT_CLIENT_EXPORT_FILE = "clients-list.json";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly HashSet<IPAddress> _bannedIps = new();

        private readonly ConcurrentDictionary<string, SRClientBase> _connectedClients =
            new();

        private readonly IEventAggregator _eventAggregator;
        private UDPVoiceRouter _serverListener;
        private ServerSync _serverSync;
        private volatile bool _stop = true;

        public ServerState(IEventAggregator eventAggregator, int port = 5002, bool portSet = false)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);

            if (portSet)
            {
                Settings.ServerSettingsStore.Instance.SetServerSetting(ServerSettingsKeys.SERVER_PORT, port);
            }

            StartServer();
        }

        public async Task HandleAsync(BanClientMessage message, CancellationToken cancellationToken)
        {
            WriteBanIP(message.Client);

            KickClient(message.Client);
        }

        public async Task HandleAsync(KickClientMessage message, CancellationToken cancellationToken)
        {
            var client = message.Client;
            KickClient(client);
        }

        public async Task HandleAsync(StartServerMessage message, CancellationToken cancellationToken)
        {
            StartServer();
            _eventAggregator.PublishOnUIThreadAsync(new ServerStateMessage(true,
                new List<SRClientBase>(_connectedClients.Values)));
        }

        public async Task HandleAsync(StopServerMessage message, CancellationToken cancellationToken)
        {
            StopServer();
            _eventAggregator.PublishOnUIThreadAsync(new ServerStateMessage(false,
                new List<SRClientBase>(_connectedClients.Values)));
        }


        private static string GetCurrentDirectory()
        {
            //To get the location the assembly normally resides on disk or the install directory
            var currentPath = AppContext.BaseDirectory;

            //once you have the path you get the directory with:
            var currentDirectory = Path.GetDirectoryName(currentPath);

            if (currentDirectory.StartsWith("file:\\")) currentDirectory = currentDirectory.Replace("file:\\", "");

            return currentDirectory;
        }

        private static string NormalizePath(string path)
        {
            // Taken from https://stackoverflow.com/a/21058121 on 2018-06-22
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private void StartServer()
        {
            if (_serverListener == null)
            {
                _serverListener = new UDPVoiceRouter(_connectedClients, _eventAggregator);
                var listenerThread = new Thread(_serverListener.Listen);
                listenerThread.Start();

                _serverSync = new ServerSync(_connectedClients, _bannedIps, _eventAggregator);
                var serverSyncThread = new Thread(_serverSync.StartListening);
                serverSyncThread.Start();
            }
        }

        public void StopServer()
        {
            if (_serverListener != null)
            {
                _stop = true;
                _serverSync.RequestStop();
                _serverSync = null;
                _serverListener.RequestStop();
                _serverListener = null;
            }
        }

        private void KickClient(SRClientBase client)
        {
            if (client != null)
                try
                {
                    ((SRSClientSession)client.ClientSession).Disconnect();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error kicking client");
                }
        }

        private void WriteBanIP(SRClientBase client)
        {
            try
            {
                var remoteIpEndPoint = ((SRSClientSession)client.ClientSession).Socket.RemoteEndPoint as IPEndPoint;

                _bannedIps.Add(remoteIpEndPoint.Address);

                File.AppendAllText(GetCurrentDirectory() + "\\banned.txt",
                    remoteIpEndPoint.Address + "\r\n");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving banned client");
            }
        }
    }
}