using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ciribob.SRS.Common.DCSState
{
    public class LatLngPosition
    {
        public double lat;
        public double lng;
        public double alt;

        public bool isValid()
        {
            return lat != 0 && lng != 0;
        }

        public override string ToString()
        {
            return $"Pos:[{lat},{lng},{alt}]";
        }
    }
}
