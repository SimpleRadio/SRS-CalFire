using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.Network.Models
{
    public class StartServerMessage
    {
    }

    public class StopServerMessage
    {
    }

    public class ServerStateMessage
    {
        private readonly List<SRClient> _srClients;

        public ServerStateMessage(bool isRunning, List<SRClient> srClients)
        {
            _srClients = srClients;
            IsRunning = isRunning;
        }

        //SUPER SAFE
        public ReadOnlyCollection<SRClient> Clients => new(_srClients);

        public bool IsRunning { get; }
        public int Count => _srClients.Count;
    }

    public class KickClientMessage
    {
        public KickClientMessage(SRClient client)
        {
            Client = client;
        }

        public SRClient Client { get; }
    }

    public class BanClientMessage
    {
        public BanClientMessage(SRClient client)
        {
            Client = client;
        }

        public SRClient Client { get; }
    }

    public class ServerSettingsChangedMessage
    {
    }

    public class ServerFrequenciesChanged
    {
        public string TestFrequencies { get; set; }
        public string GlobalLobbyFrequencies { get; set; }
    }
}