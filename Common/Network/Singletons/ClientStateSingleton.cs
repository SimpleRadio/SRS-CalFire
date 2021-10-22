using System.Collections.Generic;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Proxies;
using Ciribob.SRS.Common.PlayerState;

namespace Ciribob.SRS.Common.Network.Singletons
{
    public class ClientStateSingleton:PropertyChangedBase
    {
        private static volatile ClientStateSingleton _instance;
        private static object _lock = new object();
        private bool _isConnected;
        public ShortGuid GUID { get; }

        public PlayerUnitState PlayerUnitState { get; }

        private ClientStateSingleton()
        {
            // RadioReceivingState = new RadioReceivingState[11];
            PlayerUnitState = new PlayerUnitState();
            GUID = ShortGuid.NewGuid();
            RadioReceivingState = new RadioReceivingState[PlayerUnitState.NUMBER_OF_RADIOS];
            for (var i = 0; i < RadioReceivingState.Length; i++)
            {
                RadioReceivingState[i] = new RadioReceivingState();
            }
            RadioSendingState = new RadioSendingState();
        }

        public RadioSendingState RadioSendingState { get; set; }
        public RadioReceivingState[] RadioReceivingState { get; }

        public static ClientStateSingleton Instance
        {
            get
            {
                if (_instance == null)
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ClientStateSingleton();
                    }

                return _instance;
            }
        }

       
    }
}