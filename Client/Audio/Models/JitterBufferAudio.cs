namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Models
{
    public class JitterBufferAudio
    {
        public byte[] Audio { get; set; }

        public ulong PacketNumber { get; set; }
    }
}