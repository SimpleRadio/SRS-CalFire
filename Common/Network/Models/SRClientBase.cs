using System;
using System.Net;
using Ciribob.SRS.Common.Helpers;
using Newtonsoft.Json;

namespace Ciribob.SRS.Common.Network.Models;

public class SRClientBase : PropertyChangedBase
{
    [JsonIgnore] private float _lineOfSightLoss; // 0.0 is NO Loss therefore Full line of sight


    //TODO move all these references out to a different class!
    //These should not be here / the model doubled up on

    // Used by server client list to display last frequency client transmitted on
    private string _transmittingFrequency;
    public string ClientGuid { get; set; }

    public PlayerUnitStateBase UnitState { get; set; }
    [JsonIgnore] public bool Muted { get; set; }

    [JsonIgnore] public long LastUpdate { get; set; }

    [JsonIgnore] public IPEndPoint VoipPort { get; set; }

    [JsonIgnore] public long LastRadioUpdateSent { get; set; }

    [JsonIgnore]
    public float LineOfSightLoss
    {
        get
        {
            if (_lineOfSightLoss == 0) return 0;
            if (UnitState?.LatLng?.Lat == 0 && UnitState?.LatLng?.Lng == 0) return 0;
            return _lineOfSightLoss;
        }
        set => _lineOfSightLoss = value;
    }

    [JsonIgnore]
    public string TransmittingFrequency
    {
        get => _transmittingFrequency;
        set
        {
            if (_transmittingFrequency != value)
            {
                _transmittingFrequency = value;
                NotifyPropertyChanged();
            }
        }
    }

    // Used by server client list to remove last frequency client transmitted on after threshold
    [JsonIgnore] public DateTime LastTransmissionReceived { get; set; }

    //is an SRSClientSession but dont want to include the dependancy for now
    [JsonIgnore] public object ClientSession { get; set; }

    public override string ToString()
    {
        string side;

        if (UnitState?.Coalition == 1)
            side = "Red";
        else if (UnitState?.Coalition == 2)
            side = "Blue";
        else
            side = "Spectator";
        return UnitState?.Name == ""
            ? "Unknown"
            : UnitState.Name + " - " + side + " LOS Loss " + _lineOfSightLoss + " Pos" + UnitState?.LatLng;
    }
}