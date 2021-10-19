using System.Collections.Concurrent;

namespace Ciribob.SRS.Common.Network.Models.EventMessages
{
    public class ServerSettingsUpdatedMessage
    {
        public ConcurrentDictionary<string, string> Settings { get; }

        public ServerSettingsUpdatedMessage(ConcurrentDictionary<string, string> settings)
        {
            Settings = settings;
        }
    }
}