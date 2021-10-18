using System.Collections.Generic;
using Ciribob.SRS.Common;

namespace Ciribob.SRS.Client.Network.DCS.Models
{
    public struct CombinedRadioState
    {
        public PlayerRadioInfo RadioInfo;

        public RadioSendingState RadioSendingState;

        public RadioReceivingState[] RadioReceivingState;

        public int ClientCountConnected;

        public int[] TunedClients;
    }
}