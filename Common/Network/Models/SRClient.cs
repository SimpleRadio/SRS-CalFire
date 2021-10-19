using System;
using System.ComponentModel;
using System.Net;
using Newtonsoft.Json;

namespace Ciribob.SRS.Common.Network.Models
{
    public class SRClient : INotifyPropertyChanged
    {
        
        [JsonIgnore] 
        private float _lineOfSightLoss; // 0.0 is NO Loss therefore Full line of sight

        public string ClientGuid { get; set; }
        

        [JsonIgnore]
        public bool Muted { get; set; }

        [JsonIgnore]
        public long LastUpdate { get; set; }

        [JsonIgnore]
        public IPEndPoint VoipPort { get; set; }

        [JsonIgnore]
        public long LastRadioUpdateSent { get; set; }

        public PlayerUnitState UnitState { get; set; }

        [JsonIgnore]
        public float LineOfSightLoss
        {
            get
            {
                if (_lineOfSightLoss == 0)
                {
                    return 0;
                }
                if ((UnitState?.LatLng?.lat == 0) && (UnitState?.LatLng?.lng == 0))
                {
                    return 0;
                }
                return _lineOfSightLoss;
            }
            set { _lineOfSightLoss = value; }
        }

        // Used by server client list to display last frequency client transmitted on
        private string _transmittingFrequency;
        [JsonIgnore]
        public string TransmittingFrequency
        {
            get { return _transmittingFrequency; }
            set
            {
                if (_transmittingFrequency != value)
                {
                    _transmittingFrequency = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TransmittingFrequency"));
                }
            }
        }

        // Used by server client list to remove last frequency client transmitted on after threshold
        [JsonIgnore]
        public DateTime LastTransmissionReceived { get; set; }
        
        //is an SRSClientSession but dont want to include the dependancy for now
        [JsonIgnore]
        public object ClientSession { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            string side;

            if (UnitState?.Coalition == 1)
            {
                side = "Red";
            }
            else if (UnitState?.Coalition == 2)
            {
                side = "Blue";
            }
            else
            {
                side = "Spectator";
            }
            return UnitState?.Name == "" ? "Unknown" : UnitState.Name + " - " + side + " LOS Loss " + _lineOfSightLoss + " Pos" + UnitState?.LatLng;
        }
    }
}