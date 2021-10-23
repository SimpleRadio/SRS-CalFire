using System.Collections.Generic;

namespace Ciribob.SRS.Common.Network.Models
{

    public class PlayerUnitStateBase
    {
        public int Coalition { get; set; }

        public LatLngPosition LatLng { get; set; } = new();

        public TransponderBase Transponder { get; set; } = new();

        public string UnitType { get; set; } = "";

        public string Name { get; set; } = "";

        public uint UnitId { get; set; }

        public List<RadioBase> Radios { get; set; } = new();
        public long LastUpdate { get; set; }
    }
}