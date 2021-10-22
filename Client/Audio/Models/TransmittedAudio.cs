namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Models
{
    public class TransmittedAudio
    {
        public short[] PcmAudioShort { get; set; }
        public double Frequency { get; internal set; }
        public short Modulation { get; internal set; }
    }
}