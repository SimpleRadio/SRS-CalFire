using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ciribob.SRS.Common.DCSState;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network;
using Ciribob.SRS.Common.Network.Proxies;
using Ciribob.SRS.Common.PlayerState;
using Newtonsoft.Json;

namespace Ciribob.SRS.Common
{
    //TODO make a network proxy for PlayerRadioInfo with just the required fields

    public class PlayerUnitState :PlayerUnitStateBase
    {
   

        [JsonIgnore] private static readonly int NUMBER_OF_RADIOS = 11;

        //HOTAS or IN COCKPIT controls
        public enum RadioSwitchControls
        {
            HOTAS = 0,
            IN_COCKPIT = 1
        }

        public int Seat { get; set; }


        public bool inAircraft = false;

        public volatile bool ptt = false;

        public RadioSwitchControls control = RadioSwitchControls.HOTAS;

        public short SelectedRadio { get; set; } = 0;


        public bool IntercomHotMic { get; set; } = false;

        public static readonly uint
            UnitIdOffset = 100000000; // this is where non aircraft "Unit" Ids start from for satcom intercom

        public bool
            simultaneousTransmission =
                false; // Global toggle enabling simultaneous transmission on multiple radios, activated via the AWACS panel

        public SimultaneousTransmissionControl simultaneousTransmissionControl =
            SimultaneousTransmissionControl.ENABLED_INTERNAL_SRS_CONTROLS;

        public enum SimultaneousTransmissionControl
        {
            ENABLED_INTERNAL_SRS_CONTROLS = 1
        }

        public PlayerUnitState()
        {
            //initialise with 11 things
            //10 radios +1 intercom (the first one is intercom)
            for (var i = 0; i < NUMBER_OF_RADIOS; i++) Radios.Add(new Radio());
        }

        public long LastUpdate { get; set; }

        public void Reset()
        {
            Name = "";
            LatLng = new LatLngPosition();
            ptt = false;
            SelectedRadio = 0;
            UnitType = "";
            simultaneousTransmission = false;
            simultaneousTransmissionControl = SimultaneousTransmissionControl.ENABLED_INTERNAL_SRS_CONTROLS;
            LastUpdate = 0;

            Radios.Clear();
            for (var i = 0; i < NUMBER_OF_RADIOS; i++) Radios.Add(new Radio());
        }

        // override object.Equals
        public override bool Equals(object compare)
        {
            try
            {
                if (compare == null || GetType() != compare.GetType()) return false;

                var compareRadio = compare as PlayerUnitStateBase;
                //
                // if (control != compareRadio.control) return false;
                if (Coalition != compareRadio.Coalition) return false;
                if (!Name.Equals(compareRadio.Name)) return false;
                if (!UnitType.Equals(compareRadio.UnitType)) return false;

                if (UnitId != compareRadio.UnitId) return false;

                // if (inAircraft != compareRadio.inAircraft) return false;

                if (Transponder == null || compareRadio.Transponder == null)
                {
                    return false;
                }
                else
                {
                    //check iff
                    if (!Transponder.Equals(compareRadio.Transponder)) return false;
                }

                for (var i = 0; i < Radios.Count; i++)
                {
                    var radio1 = Radios[i];
                    var radio2 = compareRadio.Radios[i];

                    if (radio1 != null && radio2 != null)
                        if (!radio1.Equals(radio2))
                            return false;
                }
            }
            catch
            {
                return false;
            }


            return true;
        }


        /*
         * Was Radio updated in the last 10 Seconds
         */

        public bool IsCurrent()
        {
            return LastUpdate > DateTime.Now.Ticks - 100000000;
        }

     
       

        public PlayerUnitState DeepClone()
        {
            var clone = (PlayerUnitState)MemberwiseClone();

            clone.Transponder = Transponder.Copy();
            //ignore position
            clone.Radios = new List<RadioBase>();

            //TODO come back to this
          //  for (var i = 0; i < Radios.Count; i++) clone.Radios.Add(Radios[i].DeepCopy());

            return clone;
        }
    }
}