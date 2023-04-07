using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.SRS.Common.Network.Client
{
    public class ClientAudio
    {
        public byte[] EncodedAudio { get; set; }
        public float[] PcmAudioFloat { get; set; }
        public string ClientGuid { get; set; }
        public long ReceiveTime { get; set; }
        public int ReceivedRadio { get; set; }
        public double Frequency { get; set; }
        public short Modulation { get; set; }
        public float Volume { get; set; }
        public uint UnitId { get; set; }
        public short Encryption { get; internal set; }
        public bool Decryptable { get; internal set; }
        public RadioReceivingState RadioReceivingState { get; set; }
        public double RecevingPower { get; set; }
        public float LineOfSightLoss { get; set; }
        public ulong PacketNumber { get; set; }
        public string OriginalClientGuid { get; set; }
        public bool IsSecondary { get; set; }
        public bool NoAudioEffects { get; set; }
        public string UnitType { get; set; } = "";
    }
}