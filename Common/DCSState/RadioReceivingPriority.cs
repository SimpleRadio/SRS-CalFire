namespace Ciribob.SRS.Common
{
    public class RadioReceivingPriority
    {
        public double Frequency;
        public short Modulation;
        public float LineOfSightLoss;
        public double ReceivingPowerLossPercent;

        public RadioReceivingState ReceivingState;
        public RadioInformation ReceivingRadio;
    }
}