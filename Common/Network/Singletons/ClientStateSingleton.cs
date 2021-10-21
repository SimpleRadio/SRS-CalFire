using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.SRS.Common.Network.Singletons
{
    public class ClientStateSingleton
    {
        private static volatile ClientStateSingleton _instance;
        private static object _lock = new object();
        public ShortGuid GUID { get; }

        public PlayerUnitState PlayerUnitState { get; }

        private ClientStateSingleton()
        {
            // RadioReceivingState = new RadioReceivingState[11];
            PlayerUnitState = new PlayerUnitState();
            GUID = ShortGuid.NewGuid();
        }

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