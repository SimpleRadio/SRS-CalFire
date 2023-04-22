using System;

namespace Ciribob.SRS.Common.Network.Models;

public class LatLngPosition
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Alt { get; set; }

    protected bool Equals(LatLngPosition other)
    {
        return Lat.Equals(other.Lat) && Lng.Equals(other.Lng) && Alt.Equals(other.Alt);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((LatLngPosition)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Lat, Lng, Alt);
    }

    public bool IsValid()
    {
        return Lat != 0 && Lng != 0;
    }

    public override string ToString()
    {
        return $"Pos:[{Lat},{Lng},{Alt}]";
    }

    public LatLngPosition DeepClone()
    {
        return new LatLngPosition
        {
            Lat = Lat,
            Lng = Lng,
            Alt = Alt
        };
    }
}