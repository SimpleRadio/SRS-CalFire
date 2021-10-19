using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ciribob.SRS.Common.DCSState;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.PlayerState;
using Newtonsoft.Json;

namespace Ciribob.SRS.Common
{
    //TODO make a network proxy for PlayerRadioInfo with just the required fields

    public class PlayerUnitState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        private static readonly int NUMBER_OF_RADIOS = 11;

        //HOTAS or IN COCKPIT controls
        public enum RadioSwitchControls
        {
            HOTAS = 0,
            IN_COCKPIT = 1
        }

        private string _name = "";
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value == null || value == "")
                {
                    value = "---";
                }

                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
                }
            }
        }
        public int Seat { get; set; }

        private int _coalition;

        public int Coalition
        {
            get { return _coalition; }
            set
            {
                _coalition = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Coalition"));
            }
        }


        public LatLngPosition LatLng { get; set; } = new LatLngPosition();

        public Transponder Transponder { get; set; } = new Transponder();

        public bool inAircraft = false;

        public volatile bool ptt = false;

        public List<Radio> Radios { get; set; } = new List<Radio>();

        public RadioSwitchControls control = RadioSwitchControls.HOTAS;

        public short SelectedRadio { get; set; } = 0;

        public string UnitType { get; set; } = "";

        public uint UnitId { get; set; }

        public bool IntercomHotMic { get; set; } = false;

        public readonly static uint UnitIdOffset = 100000000; // this is where non aircraft "Unit" Ids start from for satcom intercom

        public bool simultaneousTransmission = false; // Global toggle enabling simultaneous transmission on multiple radios, activated via the AWACS panel

        public SimultaneousTransmissionControl simultaneousTransmissionControl =
            SimultaneousTransmissionControl.ENABLED_INTERNAL_SRS_CONTROLS;

        public enum SimultaneousTransmissionControl
        {
            ENABLED_INTERNAL_SRS_CONTROLS = 1,
        }

        public PlayerUnitState()
        {
            //initialise with 11 things
            //10 radios +1 intercom (the first one is intercom)
            for (var i = 0; i < NUMBER_OF_RADIOS; i++)
            {
                Radios.Add(new Radio());
            }
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
            for (var i = 0; i < NUMBER_OF_RADIOS; i++)
            {
                Radios.Add(new Radio());
            }

        }

        // override object.Equals
        public override bool Equals(object compare)
        {
            try
            {
                if ((compare == null) || (GetType() != compare.GetType()))
                {
                    return false;
                }

                var compareRadio = compare as PlayerUnitState;

                if (control != compareRadio.control)
                {
                    return false;
                }
                //if (side != compareRadio.side)
                //{
                //    return false;
                //}
                if (!Name.Equals(compareRadio.Name))
                {
                    return false;
                }
                if (!UnitType.Equals(compareRadio.UnitType))
                {
                    return false;
                }

                if (UnitId != compareRadio.UnitId)
                {
                    return false;
                }

                if (inAircraft != compareRadio.inAircraft)
                {
                    return false;
                }

                if (((Transponder == null) || (compareRadio.Transponder == null)))
                {
                    return false;
                }
                else
                {
                    //check iff
                    if (!Transponder.Equals(compareRadio.Transponder))
                    {
                        return false;
                    }
                }

                for (var i = 0; i < Radios.Count; i++)
                {
                    var radio1 = Radios[i];
                    var radio2 = compareRadio.Radios[i];

                    if ((radio1 != null) && (radio2 != null))
                    {
                        if (!radio1.Equals(radio2))
                        {
                            return false;
                        }
                    }
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

        //comparing doubles is risky - check that we're close enough to hear (within 100hz)
        public static bool FreqCloseEnough(double freq1, double freq2)
        {
            var diff = Math.Abs(freq1 - freq2);

            return diff < 500;
        }

        public Radio CanHearTransmission(double frequency,
            RadioConfig.Modulation modulation,
            byte encryptionKey,
            uint sendingUnitId,
            List<int> blockedRadios,
            out RadioReceivingState receivingState,
            out bool decryptable)
        {

            Radio bestMatchingRadio = null;
            RadioReceivingState bestMatchingRadioState = null;
            bool bestMatchingDecryptable = false;

            for (var i = 0; i < Radios.Count; i++)
            {
                var receivingRadio = Radios[i];

                if (receivingRadio != null)
                {
                    if (modulation == RadioConfig.Modulation.DISABLED
                        || receivingRadio.Modulation == RadioConfig.Modulation.DISABLED)
                    {
                        continue;
                    }

                    //handle INTERCOM Modulation is 2
                    if ((receivingRadio.Modulation == RadioConfig.Modulation.INTERCOM) &&
                        (modulation == RadioConfig.Modulation.INTERCOM))
                    {
                        if ((UnitId > 0) && (sendingUnitId > 0)
                            && (UnitId == sendingUnitId) )
                        {
                            receivingState = new RadioReceivingState
                            {
                                IsSecondary = false,
                                LastReceviedAt = DateTime.Now.Ticks,
                                ReceivedOn = i
                            };
                            decryptable = true;
                            return receivingRadio;
                        }
                        decryptable = false;
                        receivingState = null;
                        return null;
                    }

                    //within 1khz
                    if ((FreqCloseEnough(receivingRadio.Frequency,frequency))
                        && (receivingRadio.Modulation == modulation)
                        && (receivingRadio.Frequency > 10000))
                    {
                        bool isDecryptable = true;

                        if (isDecryptable && !blockedRadios.Contains(i))
                        {
                            receivingState = new RadioReceivingState
                            {
                                IsSecondary = false,
                                LastReceviedAt = DateTime.Now.Ticks,
                                ReceivedOn = i
                            };
                            decryptable = true;
                            return receivingRadio;
                        }

                        bestMatchingRadio = receivingRadio;
                        bestMatchingRadioState = new RadioReceivingState
                        {
                            IsSecondary = false,
                            LastReceviedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                        bestMatchingDecryptable = isDecryptable;
                    }
                    if ((receivingRadio.SecondaryFrequency == frequency)
                        && (receivingRadio.SecondaryFrequency > 10000))
                    {
                        if (encryptionKey == 0 || (receivingRadio.Encrypted ? receivingRadio.EncryptionKey : (byte)0) == encryptionKey)
                        {
                            receivingState = new RadioReceivingState
                            {
                                IsSecondary = true,
                                LastReceviedAt = DateTime.Now.Ticks,
                                ReceivedOn = i
                            };
                            decryptable = true;
                            return receivingRadio;
                        }

                        bestMatchingRadio = receivingRadio;
                        bestMatchingRadioState = new RadioReceivingState
                        {
                            IsSecondary = true,
                            LastReceviedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                    }
                }
            }

            decryptable = bestMatchingDecryptable;
            receivingState = bestMatchingRadioState;
            return bestMatchingRadio;
        }

        public PlayerUnitState DeepClone()
        {
            var clone = (PlayerUnitState) this.MemberwiseClone();

            clone.Transponder = this.Transponder.Copy();
            //ignore position
            clone.Radios = new List<Radio>();

            for (var i = 0; i < this.Radios.Count; i++)
            {
                clone.Radios.Add(this.Radios[i].DeepCopy());
            }

            return clone;

        }
    }
}