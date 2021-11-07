using System;
using System.Collections.Generic;

namespace Ciribob.SRS.Common.Network.Models
{
    public class RadioBase
    {
        protected bool Equals(RadioBase other)
        {
            return FreqCloseEnough(Freq,other.Freq) 
                   && Modulation == other.Modulation 
                   && Encrypted == other.Encrypted 
                   && EncKey == other.EncKey 
                   && FreqCloseEnough(SecFreq, other.SecFreq);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RadioBase)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Freq, (int)Modulation, Encrypted, EncKey, SecFreq);
        }

        public double Freq { get; set; } = 1;
        public Modulation Modulation { get; set; } = Modulation.DISABLED;

        public bool Encrypted { get; set; } = false;
        public byte EncKey { get; set; } = 0;

        public double SecFreq { get; set; } = 1;
        public static RadioBase CanHearTransmission(double frequency,
            Modulation modulation,
            byte encryptionKey,
            uint sendingUnitId,
            List<int> blockedRadios,
            List<RadioBase> receivingRadios,
            uint receivingUnitId,
            out RadioReceivingState receivingState,
            out bool decryptable)
        {
            RadioBase bestMatchingRadio = null;
            RadioReceivingState bestMatchingRadioState = null;
            var bestMatchingDecryptable = false;

            for (var i = 0; i < receivingRadios.Count; i++)
            {
                var receivingRadio = receivingRadios[i];

                if (receivingRadio != null)
                {
                    if (modulation == Modulation.DISABLED
                        || receivingRadio.Modulation == Modulation.DISABLED)
                        continue;

                    //handle INTERCOM Modulation is 2
                    if (receivingRadio.Modulation == Modulation.INTERCOM &&
                        modulation == Modulation.INTERCOM)
                    {
                        if (receivingUnitId > 0 && sendingUnitId > 0
                                                && receivingUnitId == sendingUnitId)
                        {
                            receivingState = new RadioReceivingState
                            {
                                IsSecondary = false,
                                LastRecievedAt = DateTime.Now.Ticks,
                                ReceivedOn = i
                            };
                            decryptable = true;
                            return receivingRadio;
                        }

                        decryptable = false;
                        receivingState = null;
                        return null;
                    }

                    //within 1khz
                    if (FreqCloseEnough(receivingRadio.Freq, frequency)
                        && receivingRadio.Modulation == modulation
                        && receivingRadio.Freq > 10000)
                    {
                        var isDecryptable = true;

                        if (isDecryptable && !blockedRadios.Contains(i))
                        {
                            receivingState = new RadioReceivingState
                            {
                                IsSecondary = false,
                                LastRecievedAt = DateTime.Now.Ticks,
                                ReceivedOn = i
                            };
                            decryptable = true;
                            return receivingRadio;
                        }

                        bestMatchingRadio = receivingRadio;
                        bestMatchingRadioState = new RadioReceivingState
                        {
                            IsSecondary = false,
                            LastRecievedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                        bestMatchingDecryptable = isDecryptable;
                    }

                    if (receivingRadio.SecFreq == frequency
                        && receivingRadio.SecFreq > 10000)
                    {
                        //TODO come back too
                        if (encryptionKey == 0 || (receivingRadio.Encrypted ? receivingRadio.EncKey : (byte)0) ==
                            encryptionKey)
                        {
                            receivingState = new RadioReceivingState
                            {
                                IsSecondary = true,
                                LastRecievedAt = DateTime.Now.Ticks,
                                ReceivedOn = i
                            };
                            decryptable = true;
                            return receivingRadio;
                        }

                        bestMatchingRadio = receivingRadio;
                        bestMatchingRadioState = new RadioReceivingState
                        {
                            IsSecondary = true,
                            LastRecievedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                    }
                }
            }

            decryptable = bestMatchingDecryptable;
            receivingState = bestMatchingRadioState;
            return bestMatchingRadio;
        }

        //comparing doubles is risky - check that we're close enough to hear (within 100hz)

        public static bool FreqCloseEnough(double freq1, double freq2)
        {
            var diff = Math.Abs(freq1 - freq2);

            return diff < 500;
        }

        public RadioBase DeepClone()
        {
            return new RadioBase()
            {
                Encrypted = Encrypted,
                Modulation = Modulation,
                SecFreq = SecFreq,
                EncKey = EncKey,
                Freq = Freq
            };
        }
    }
}