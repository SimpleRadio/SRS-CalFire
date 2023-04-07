using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.SRS.Common.Network.Client
{
    public class ClientAudio
    {
        public byte[] EncodedAudio { get; set; }
        public short[] PcmAudioShort { get; set; }
        public string ClientGuid { get; set; }
        public long ReceiveTime { get; set; }
        public int ReceivedRadio { get; set; }
        public double Frequency { get; set; }
        public short Modulation { get; set; }
        public float Volume { get; set; }
        public uint UnitId { get; set; }
        public RadioReceivingState RadioReceivingState { get; set; }
        public double RecevingPower { get; set; }
        public float LineOfSightLoss { get; set; }
        public ulong PacketNumber { get; set; }
        public string OriginalClientGuid { get; set; }
        public string UnitType { get; set; } = "";
    }
}