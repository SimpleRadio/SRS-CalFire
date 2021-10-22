using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ciribob.SRS.Common.PlayerState;

namespace Ciribob.SRS.Common.Network.Proxies
{
    public class RadioBase
    {
        public double Freq { get; set; } = 1;
        public Modulation Modulation { get; set; } = Modulation.DISABLED;

        public bool Encrypted { get; set; } = false;
        public byte EncKey { get; set; } = 0;

        public double SecFreq { get; set; } = 1;
    }
}