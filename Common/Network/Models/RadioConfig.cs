namespace Ciribob.SRS.Common.Network.Models
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
        public double GuardFrequency { get; set; } = 1;
        public bool HotMic { get; set; }
    }
}