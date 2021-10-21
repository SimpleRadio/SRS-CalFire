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
        public double Frequency { get; set; } = 1;
        public Modulation Modulation { get; set; } = Modulation.DISABLED;
        public string Name { get; set; } = "";
        public double SecondaryFrequency { get; set; } = 1;
    }
}