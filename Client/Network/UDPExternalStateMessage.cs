using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Network
{
    public class UDPExternalStateMessage
    {
        public List<UDPExternalRadioState> Radios { get; set; }

        public uint UnitId { get; set; }

        public string Name { get; set; }

        public int SelectedRadio { get; set; }

    }
}
