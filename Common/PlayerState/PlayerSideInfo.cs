using Ciribob.SRS.Common.DCSState;
using Ciribob.SRS.Common.PlayerState;

namespace Ciribob.SRS.Common
{
    public class PlayerSideInfo
    {
        public string name = "";
        public int side = 0;
        public int seat = 0; // 0 is front / normal - 1 is back seat

        public LatLngPosition LngLngPosition { get; set; } = new LatLngPosition();

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj is PlayerSideInfo info &&
                   name == info.name &&
                   side == info.side &&
                   seat == info.seat;
        }

        public void Reset()
        {
            name = "";
            side = 0;
            seat = 0;
            LngLngPosition = new LatLngPosition();
        }



    }
}