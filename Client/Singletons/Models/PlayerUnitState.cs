using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Annotations;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Singletons;
using Newtonsoft.Json;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons.Models
{

    public class PlayerUnitState : PropertyChangedBase
    {
        private static readonly string HANDHELD_RADIO_JSON = "handheld-radio.json";

        //HOTAS or IN COCKPIT controls
        public enum RadioSwitchControls
        {
            HOTAS = 0,
            IN_COCKPIT = 1
        }

        public enum SimultaneousTransmissionControl
        {
            ENABLED_INTERNAL_SRS_CONTROLS = 1
        }

        [JsonIgnore] public static readonly int NUMBER_OF_RADIOS = 11;

        public static readonly uint
            UnitIdOffset = 100000000; // this is where non aircraft "Unit" Ids start from for satcom intercom

        public RadioSwitchControls control = RadioSwitchControls.HOTAS;


        public bool InAircraft { get; set; }

        public int Coalition { get; set; }

        public LatLngPosition LatLng { get; set; } = new();

        public Transponder Transponder { get; set; } = new();

        public string UnitType { get; set; } = "";

        public string Name { get; set; } = "";

        public uint UnitId { get; set; }


        public bool
            simultaneousTransmission; // Global toggle enabling simultaneous transmission on multiple radios, activated via the AWACS panel

        public SimultaneousTransmissionControl simultaneousTransmissionControl =
            SimultaneousTransmissionControl.ENABLED_INTERNAL_SRS_CONTROLS;

        public PlayerUnitState()
        {
            //initialise with 11 things
            //10 radios +1 intercom (the first one is intercom)
            //try the handheld radios first
            foreach (var radio in RadioHelper.LoadRadioConfig(HANDHELD_RADIO_JSON))
                Radios.Add(radio);

            SelectedRadio = 1;
        }

        public int Seat { get; set; }

        public short SelectedRadio { get; set; } 

        public bool IntercomHotMic { get; set; } = false;

        public ObservableCollection<Radio> Radios { get; private set; } = new();

        public List<RadioBase> BaseRadios
        {
            get
            {
                List<RadioBase> radios = new List<RadioBase>();
                foreach (var radio in Radios)
                {
                    radios.Add(radio.RadioBase);
                    
                }

                return radios;
            }
        }

        public PlayerUnitStateBase PlayerUnitStateBase
        {
            get
            {
                return new PlayerUnitStateBase()
                {
                    Coalition = Coalition,
                    Name = Name,
                    LatLng = LatLng,
                    Radios = BaseRadios,
                    Transponder = Transponder,
                    UnitId = UnitId,
                    UnitType = UnitType
                };
            }
        }
        //
        // public void Reset()
        // {
        //     Name = "";
        //     LatLng = new LatLngPosition();
        //     SelectedRadio = 0;
        //     UnitType = "";
        //     simultaneousTransmission = false;
        //     simultaneousTransmissionControl = SimultaneousTransmissionControl.ENABLED_INTERNAL_SRS_CONTROLS;
        //     LastUpdate = 0;
        //
        //     Radios.Clear();
        //     for (var i = 0; i < NUMBER_OF_RADIOS; i++) Radios.Add(new Radio());
        // }


    }
}