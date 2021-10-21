namespace Ciribob.SRS.Common.PlayerState
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