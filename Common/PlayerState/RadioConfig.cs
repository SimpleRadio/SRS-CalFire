namespace Ciribob.SRS.Common.PlayerState
{
    public class RadioConfig
    {
        public enum EncryptionMode
        {
            NO_ENCRYPTION = 0,
            ENCRYPTION_JUST_OVERLAY = 1,
            ENCRYPTION_FULL = 2,
            ENCRYPTION_COCKPIT_TOGGLE_OVERLAY_CODE = 3

            // 0  is no controls
            // 1 is FC3 Gui Toggle + Gui Enc key setting
            // 2 is InCockpit toggle + Incockpit Enc setting
            // 3 is Incockpit toggle + Gui Enc Key setting
        }

        public enum FreqMode
        {
            COCKPIT = 0,
            OVERLAY = 1
        }

        public enum RetransmitMode
        {
            COCKPIT = 0,
            OVERLAY = 1,
            DISABLED = 2
        }

        public enum VolumeMode
        {
            COCKPIT = 0,
            OVERLAY = 1
        }


        public RetransmitMode RetransmitControl { get; set; } = RetransmitMode.DISABLED;
        public FreqMode FrequencyControl { get; set; } = FreqMode.COCKPIT;
        public FreqMode GuardFrequencyControl { get; set; } = FreqMode.COCKPIT;
        public VolumeMode VolumeControl { get; set; } = VolumeMode.COCKPIT;

        public EncryptionMode EncryptionControl { get; set; } = EncryptionMode.NO_ENCRYPTION;

        public double MaxFrequency { get; set; } = 1;
        public double MinimumFrequency { get; set; } = 1;

        public RadioConfig DeepCopy()
        {
            return new RadioConfig
            {
                MaxFrequency = MaxFrequency,
                MinimumFrequency = MinimumFrequency,
                VolumeControl = VolumeControl,
                GuardFrequencyControl = GuardFrequencyControl,
                FrequencyControl = FrequencyControl,
                RetransmitControl = RetransmitControl,
                EncryptionControl = EncryptionControl
            };
        }

        protected bool Equals(RadioConfig other)
        {
            return RetransmitControl == other.RetransmitControl
                   && FrequencyControl == other.FrequencyControl
                   && GuardFrequencyControl == other.GuardFrequencyControl
                   && VolumeControl == other.VolumeControl
                   && EncryptionControl == other.EncryptionControl
                   && MaxFrequency.Equals(other.MaxFrequency)
                   && MinimumFrequency.Equals(other.MinimumFrequency);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return Equals((RadioConfig)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)RetransmitControl;
                hashCode = (hashCode * 397) ^ (int)FrequencyControl;
                hashCode = (hashCode * 397) ^ (int)GuardFrequencyControl;
                hashCode = (hashCode * 397) ^ (int)VolumeControl;
                hashCode = (hashCode * 397) ^ (int)EncryptionControl;
                hashCode = (hashCode * 397) ^ MaxFrequency.GetHashCode();
                hashCode = (hashCode * 397) ^ MinimumFrequency.GetHashCode();
                return hashCode;
            }
        }
    }
}