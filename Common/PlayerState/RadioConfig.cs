using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ciribob.SRS.Common.PlayerState
{
    public class RadioConfig
    {
        public enum VolumeMode
        {
            COCKPIT = 0,
            OVERLAY = 1,
        }

        public enum FreqMode
        {
            COCKPIT = 0,
            OVERLAY = 1,
        }

        public enum RetransmitMode
        {
            COCKPIT = 0,
            OVERLAY = 1,
            DISABLED = 2,
        }

        public enum Modulation
        {
            AM = 0,
            FM = 1,
            INTERCOM = 2,
            DISABLED = 3,
            HAVEQUICK = 4,
            SATCOM = 5,
            MIDS = 6,
        }

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


        public RetransmitMode RetransmitControl { get; set; } = RadioConfig.RetransmitMode.DISABLED;
        public FreqMode FrequencyControl { get; set; } = RadioConfig.FreqMode.COCKPIT;
        public FreqMode GuardFrequencyControl { get; set; } = RadioConfig.FreqMode.COCKPIT;
        public VolumeMode VolumeControl { get; set; } = RadioConfig.VolumeMode.COCKPIT;

        public EncryptionMode EncryptionControl { get; set; } = RadioConfig.EncryptionMode.NO_ENCRYPTION;

        public double MaxFrequency { get; set; } = 1;
        public double MinimumFrequency { get; set; } = 1;

        internal RadioConfig DeepCopy()
        {

            return new RadioConfig()
            {
                MaxFrequency = this.MaxFrequency,
                MinimumFrequency = this.MinimumFrequency,
                VolumeControl = this.VolumeControl,
                GuardFrequencyControl = this.GuardFrequencyControl,
                FrequencyControl = this.FrequencyControl,
                RetransmitControl = this.RetransmitControl,
                EncryptionControl = this.EncryptionControl

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
            if ((obj == null) || (GetType() != obj.GetType()))
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
