using System.Collections.Generic;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons.Models;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Singletons;
using Ciribob.SRS.Common.PlayerState;
using Ciribob.SRS.Common.Setting;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons
{
    public class ClientStateSingleton : PropertyChangedBase
    {
        private static volatile ClientStateSingleton _instance;
        private static readonly object _lock = new();
        private bool _isConnected;

        private ClientStateSingleton()
        {
            // RadioReceivingState = new RadioReceivingState[11];
            PlayerUnitState = new PlayerUnitState();
            GUID = ShortGuid.NewGuid();
            RadioReceivingState = new RadioReceivingState[PlayerUnitState.NUMBER_OF_RADIOS];
            for (var i = 0; i < RadioReceivingState.Length; i++) RadioReceivingState[i] = new RadioReceivingState();

            RadioSendingState = new RadioSendingState();
        }

        public ShortGuid GUID { get; }

        public PlayerUnitState PlayerUnitState { get; }

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


        public int ClientsOnFreq(double freq, Modulation modulation)
        {
            if (!SyncedServerSettings.Instance.GetSettingAsBool(ServerSettingsKeys.SHOW_TUNED_COUNT))
                //TODO make this client side controlled
                return 0;

            var currentUnitId = Instance.PlayerUnitState.UnitId;

            var count = 0;

            foreach (var client in ConnectedClientsSingleton.Instance.Clients)
                if (!client.Key.Equals(GUID))
                {
                    var radioInfo = client.Value.UnitState;

                    if (radioInfo != null)
                    {
                        RadioReceivingState radioReceivingState = null;
                        bool decryptable;
                        var receivingRadio = radioInfo.CanHearTransmission(freq,
                            modulation,
                            0,
                            currentUnitId,
                            new List<int>(),
                            out radioReceivingState,
                            out decryptable);

                        //only send if we can hear!
                        if (receivingRadio != null) count++;
                    }
                }

            return count;
        }
    }
}