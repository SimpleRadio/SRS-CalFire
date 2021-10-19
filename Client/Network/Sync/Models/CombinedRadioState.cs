using System.Collections.Generic;
using Ciribob.SRS.Common;

namespace Ciribob.SRS.Client.Network.Sync.Models
{
    public struct CombinedRadioState
    {
        public PlayerUnitState unitState;

        public RadioSendingState RadioSendingState;

        public RadioReceivingState[] RadioReceivingState;

        public int ClientCountConnected;

        public int[] TunedClients;
    }
}