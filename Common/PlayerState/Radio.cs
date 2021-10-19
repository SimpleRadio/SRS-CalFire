using System;
using System.Collections.Generic;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings.RadioChannels;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.PlayerState;
using Newtonsoft.Json;

namespace Ciribob.SRS.Common
{
    public class Radio
    {
        public RadioConfig Config { get; set; } = new RadioConfig();

        public bool Encrypted { get; set; } = false;
        public byte EncryptionKey { get; set; } = 0;

        public double Frequency { get; set; } = 1;
        public RadioConfig.Modulation Modulation { get; set; } = RadioConfig.Modulation.DISABLED;
        public string Name { get; set; } = "";
        public double SecondaryFrequency { get; set; } = 1;

        //should the radio restransmit?
        public bool Retransmit { get; set; } = false;
        public float Volume { get; set; } = 1.0f;
        public int CurrentChannel { get; set; } = -1;
        public bool SimultaneousTransmission { get; set; } = false;

        //Channels
        public List<PresetChannel> PresetChannels { get; }

        /**
         * Used to determine if we should send an update to the server or not
         * We only need to do that if something that would stop us Receiving happens which
         * is frequencies and modulation
         */

        public bool Available()
        {
            return Modulation != RadioConfig.Modulation.DISABLED;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || (GetType() != obj.GetType()))
                return false;

            var compare = (Radio) obj;

            if (!Name.Equals(compare.Name))
            {
                return false;
            }
            if (!PlayerUnitState.FreqCloseEnough(Frequency , compare.Frequency))
            {
                return false;
            }
            if (Modulation != compare.Modulation)
            {
                return false;
            }
            if (Encrypted != compare.Encrypted)
            {
                return false;
            }
            if (EncryptionKey != compare.EncryptionKey)
            {
                return false;
            }
            if (Retransmit != compare.Retransmit)
            {
                return false;
            }
            if (!PlayerUnitState.FreqCloseEnough(SecondaryFrequency, compare.SecondaryFrequency))
            {
                return false;
            }

            if (Config != null && compare.Config == null)
            {
                return false;
            }
            if (Config == null && compare.Config != null)
            {
                return false;
            }

            return Config.Equals(compare.Config);
        }

        internal Radio DeepCopy()
        {
            //probably can use memberswise clone
            return new Radio()
            {
                CurrentChannel = this.CurrentChannel,
                Encrypted = this.Encrypted,
                EncryptionKey = this.EncryptionKey,
                Frequency = this.Frequency,
                Modulation = this.Modulation,
                SecondaryFrequency = this.SecondaryFrequency,
                Name = this.Name,
                SimultaneousTransmission = this.SimultaneousTransmission,
                Volume = this.Volume,
                Retransmit = this.Retransmit,
                Config = this.Config?.DeepCopy()

            };
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Config != null ? Config.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Encrypted.GetHashCode();
                hashCode = (hashCode * 397) ^ EncryptionKey.GetHashCode();
                hashCode = (hashCode * 397) ^ Frequency.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Modulation;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SecondaryFrequency.GetHashCode();
                hashCode = (hashCode * 397) ^ Retransmit.GetHashCode();
                hashCode = (hashCode * 397) ^ Volume.GetHashCode();
                hashCode = (hashCode * 397) ^ CurrentChannel;
                hashCode = (hashCode * 397) ^ SimultaneousTransmission.GetHashCode();
                hashCode = (hashCode * 397) ^ (PresetChannels != null ? PresetChannels.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}