namespace Ciribob.SRS.Common.PlayerState
{
    public class LatLngPosition
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Alt { get; set; }

        public bool IsValid()
        {
            return Lat != 0 && Lng != 0;
        }

        public override string ToString()
        {
            return $"Pos:[{Lat},{Lng},{Alt}]";
        }
    }
}