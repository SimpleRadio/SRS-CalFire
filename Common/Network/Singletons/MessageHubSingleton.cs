using System;
using Easy.MessageHub;

namespace Ciribob.SRS.Common.Network.Singletons
{
    public class MessageHubSingleton
    {
        private static MessageHubSingleton _instance;
        private static object _lock = new object();

        private MessageHub _messageHub;

        private MessageHubSingleton()
        {
            _messageHub = new MessageHub();
        }

        public static MessageHub Instance
        {
            get
            {
                if (_instance == null)
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new MessageHubSingleton();
                    }

                return _instance._messageHub;
            }
        }
    }
}