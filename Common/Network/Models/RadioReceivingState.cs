using System;

namespace Ciribob.SRS.Common.Network.Models
{
    public class RadioReceivingState
    {
        public long LastReceviedAt { get; set; }

        public bool IsSecondary { get; set; }
        public bool IsSimultaneous { get; set; }
        public int ReceivedOn { get; set; }

        public bool PlayedEndOfTransmission { get; set; }

        public string SentBy { get; set; }

        public bool IsReceiving => DateTime.Now.Ticks - LastReceviedAt < 3500000;
    }
}