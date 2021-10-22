using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ciribob.SRS.Common.DCSState;
using Ciribob.SRS.Common.Network.Proxies;
using Ciribob.SRS.Common.PlayerState;

namespace Ciribob.SRS.Common.Network
{
    //TODO - proxy for playerState just for the network

    public class PlayerUnitStateBase
    {
        public int Coalition { get; set; }

        public LatLngPosition LatLng { get; set; } = new LatLngPosition();

        public TransponderBase Transponder { get; set; } = new TransponderBase();

        public string UnitType { get; set; } = "";

        public string Name { get; set; } = "";

        public uint UnitId { get; set; }

        public List<RadioBase> Radios { get; set; } = new List<RadioBase>();
        public long LastUpdate { get; set; }

        public RadioBase CanHearTransmission(double frequency,
           Modulation modulation,
           byte encryptionKey,
           uint sendingUnitId,
           List<int> blockedRadios,
           out RadioReceivingState receivingState,
           out bool decryptable)
        {
            RadioBase bestMatchingRadio = null;
            RadioReceivingState bestMatchingRadioState = null;
            var bestMatchingDecryptable = false;

            for (var i = 0; i < Radios.Count; i++)
            {
                var receivingRadio = Radios[i];

                if (receivingRadio != null)
                {
                    if (modulation == Modulation.DISABLED
                        || receivingRadio.Modulation == Modulation.DISABLED)
                        continue;

                    //handle INTERCOM Modulation is 2
                    if (receivingRadio.Modulation == Modulation.INTERCOM &&
                        modulation == Modulation.INTERCOM)
                    {
                        if (UnitId > 0 && sendingUnitId > 0
                                       && UnitId == sendingUnitId)
                        {
                            receivingState = new RadioReceivingState
                            {
                                IsSecondary = false,
                                LastReceviedAt = DateTime.Now.Ticks,
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
                                LastReceviedAt = DateTime.Now.Ticks,
                                ReceivedOn = i
                            };
                            decryptable = true;
                            return receivingRadio;
                        }

                        bestMatchingRadio = receivingRadio;
                        bestMatchingRadioState = new RadioReceivingState
                        {
                            IsSecondary = false,
                            LastReceviedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                        bestMatchingDecryptable = isDecryptable;
                    }

                    if (receivingRadio.SecFreq == frequency
                        && receivingRadio.SecFreq > 10000)
                    {
                        //TODO come back too
                        // if (encryptionKey == 0 || (receivingRadio.Encrypted ? receivingRadio.EncryptionKey : (byte)0) ==
                        //     encryptionKey)
                        // {
                        //     receivingState = new RadioReceivingState
                        //     {
                        //         IsSecondary = true,
                        //         LastReceviedAt = DateTime.Now.Ticks,
                        //         ReceivedOn = i
                        //     };
                        //     decryptable = true;
                        //     return receivingRadio;
                        // }

                        bestMatchingRadio = receivingRadio;
                        bestMatchingRadioState = new RadioReceivingState
                        {
                            IsSecondary = true,
                            LastReceviedAt = DateTime.Now.Ticks,
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
    }
}