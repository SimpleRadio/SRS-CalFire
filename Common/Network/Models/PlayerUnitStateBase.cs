using System;
using System.Collections.Generic;
using System.Linq;

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

        protected bool Equals(PlayerUnitStateBase other)
        {
            return Coalition == other.Coalition 
                   && LatLng.Equals(other)
                   && Transponder.Equals(other.Transponder)
                   && UnitType == other.UnitType 
                   && Name == other.Name 
                   && UnitId == other.UnitId 
                   && RadiosEqual(other.Radios);
        }

        private bool RadiosEqual(List<RadioBase> otherRadios)
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.except?view=net-5.0
            // With link we can return all non matching elements which is cool
            //just need to override equals and hashcode
            var listCompare = Radios.Except(otherRadios);

            foreach (var product in listCompare)
            {
                if(product!=null)
                    return false;
            }
            
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PlayerUnitStateBase)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Coalition, LatLng, Transponder, UnitType, Name, UnitId, Radios);
        }

        public bool MetaDataEquals(PlayerUnitStateBase other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != this.GetType()) return false;

            return Coalition == other.Coalition
                   && LatLng.Equals(other)
                   && Transponder.Equals(other.Transponder)
                   && UnitType == other.UnitType
                   && Name == other.Name
                   && UnitId == other.UnitId;
        }

        public PlayerUnitStateBase UpdateMetadata(PlayerUnitStateBase other)
        {
            Coalition = other.Coalition;
            LatLng = other.LatLng;
            Transponder = other.Transponder;
            UnitType = other.UnitType;
            Name = other.Name;
            UnitId = other.UnitId;

            return this;
        }

        public PlayerUnitStateBase DeepClone()
        {
            var copy = new PlayerUnitStateBase()
            {
                Coalition = Coalition,
                LatLng = LatLng.DeepClone(),
                Transponder = Transponder.DeepClone(),
                UnitType = UnitType,
                Name = Name,
                UnitId = UnitId,
            };

            var radios = new List<RadioBase>();

            foreach (var radio in Radios)
            {
                radios.Add(radio.DeepClone());
            }

            copy.Radios = radios;

            return copy;
        }
    }
}