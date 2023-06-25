using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Models;

//TODO profile if its better as class or struct
public struct DeJitteredTransmission
{
    public int ReceivedRadio { get; set; }
    public Modulation Modulation { get; set; }
    public float Volume { get; set; }
    public bool IsSecondary { get; set; }
    public double Frequency { get; set; }
    public float[] PCMMonoAudio { get; set; }
    public int PCMAudioLength { get; set; }
    public string Guid { get; set; }
    public string OriginalClientGuid { get; set; }
    public string UnitType { get; set; }
}