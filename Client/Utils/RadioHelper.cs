using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ciribob.SRS.Client.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.PresetChannels;
using Ciribob.SRS.Common;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Utils
{
    public static class RadioHelper
    {
        public static void ToggleGuard(int radioId)
        {
            var radio = GetRadio(radioId);

            if (radio != null)
            {
                if (radio.freqMode == RadioInformation.FreqMode.OVERLAY || radio.guardFreqMode == RadioInformation.FreqMode.OVERLAY)
                {
                    if (radio.secFreq > 0)
                    {
                        radio.secFreq = 0; // 0 indicates we want it overridden + disabled
                    }
                    else
                    {
                        radio.secFreq = 1; //indicates we want it back
                    }

                    //make radio data stale to force resysnc
                    ClientStateSingleton.Instance.LastSent = 0;
                }
            }
        }

        public static void SetGuard(int radioId, bool enabled)
        {
            var radio = GetRadio(radioId);

            if (radio != null)
            {
                if (radio.freqMode == RadioInformation.FreqMode.OVERLAY || radio.guardFreqMode == RadioInformation.FreqMode.OVERLAY)
                {
                    if (!enabled)
                    {
                        radio.secFreq = 0; // 0 indicates we want it overridden + disabled
                    }
                    else
                    {
                        radio.secFreq = 1; //indicates we want it back
                    }

                    //make radio data stale to force resysnc
                    ClientStateSingleton.Instance.LastSent = 0;
                }
            }
        }

        public static bool UpdateRadioFrequency(double frequency, int radioId, bool delta = true, bool inMHz = true)
        {
            bool inLimit = true;
            const double MHz = 1000000;

            if (inMHz)
            {
                frequency = frequency * MHz;
            }

            var radio = GetRadio(radioId);

            if (radio != null)
            {
                if (radio.modulation != RadioInformation.Modulation.DISABLED
                    && radio.modulation != RadioInformation.Modulation.INTERCOM
                    && radio.freqMode == RadioInformation.FreqMode.OVERLAY)
                {
                    if (delta)
                    {
                        radio.freq = (int)Math.Round(radio.freq + frequency);
                    }
                    else
                    {
                        radio.freq = (int)Math.Round(frequency);
                    }

                    //make sure we're not over or under a limit
                    if (radio.freq > radio.freqMax)
                    {
                        inLimit = false;
                        radio.freq = radio.freqMax;
                    }
                    else if (radio.freq < radio.freqMin)
                    {
                        inLimit = false;
                        radio.freq = radio.freqMin;
                    }

                    //set to no channel
                    radio.channel = -1;

                    //make radio data stale to force resysnc
                    ClientStateSingleton.Instance.LastSent = 0;
                }
            }
            return inLimit;
        }

        public static bool SelectRadio(int radioId)
        {
            var radio = GetRadio(radioId);

            if (radio != null)
            {
                if (radio.modulation != RadioInformation.Modulation.DISABLED
                    && ClientStateSingleton.Instance.PlayerRadioInfo.control ==
                    PlayerRadioInfo.RadioSwitchControls.HOTAS)
                {
                    ClientStateSingleton.Instance.PlayerRadioInfo.selected = (short) radioId;
                    return true;
                }
            }

            return false;
        }

        public static RadioInformation GetRadio(int radio)
        {
            var dcsPlayerRadioInfo = ClientStateSingleton.Instance.PlayerRadioInfo;

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent() &&
                radio < dcsPlayerRadioInfo.radios.Length && (radio >= 0))
            {
                return dcsPlayerRadioInfo.radios[radio];
            }

            return null;
        }



        public static void SelectNextRadio()
        {
            var dcsPlayerRadioInfo = ClientStateSingleton.Instance.PlayerRadioInfo;

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent() &&
                dcsPlayerRadioInfo.control == PlayerRadioInfo.RadioSwitchControls.HOTAS)
            {
                if (dcsPlayerRadioInfo.selected < 0
                    || dcsPlayerRadioInfo.selected > dcsPlayerRadioInfo.radios.Length
                    || dcsPlayerRadioInfo.selected + 1 > dcsPlayerRadioInfo.radios.Length)
                {
                    SelectRadio(1);

                    return;
                }
                else
                {
                    int currentRadio = dcsPlayerRadioInfo.selected;

                    //find next radio
                    for (int i = currentRadio + 1; i < dcsPlayerRadioInfo.radios.Length; i++)
                    {
                        if (SelectRadio(i))
                        {
                            return;
                        }
                    }

                    //search up to current radio
                    for (int i = 1; i < currentRadio; i++)
                    {
                        if (SelectRadio(i))
                        {
                            return;
                        }
                    }
                }
            }
        }

        public static void SelectPreviousRadio()
        {
            var dcsPlayerRadioInfo = ClientStateSingleton.Instance.PlayerRadioInfo;

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent() &&
                dcsPlayerRadioInfo.control == PlayerRadioInfo.RadioSwitchControls.HOTAS)
            {
                if (dcsPlayerRadioInfo.selected < 0
                    || dcsPlayerRadioInfo.selected > dcsPlayerRadioInfo.radios.Length)
                {
                    dcsPlayerRadioInfo.selected = 1;
                    return;
                }
                else
                {
                    int currentRadio = dcsPlayerRadioInfo.selected;

                    //find previous radio
                    for (int i = currentRadio - 1; i > 0; i--)
                    {
                        if (SelectRadio(i))
                        {
                            return;
                        }
                    }

                    //search down to current radio
                    for (int i = dcsPlayerRadioInfo.radios.Length; i > currentRadio; i--)
                    {
                        if (SelectRadio(i))
                        {
                            return;
                        }
                    }
                }
            }
        }

     
        public static void SelectRadioChannel(PresetChannel selectedPresetChannel, int radioId)
        {
            if (UpdateRadioFrequency((double) selectedPresetChannel.Value, radioId, false, false))
            {
                var radio = GetRadio(radioId);

                if (radio != null) radio.channel = selectedPresetChannel.Channel;
            }
        }

        public static void RadioChannelUp(int radioId)
        {
            var currentRadio = RadioHelper.GetRadio(radioId);

            if (currentRadio != null)
            {
                if (currentRadio.modulation != RadioInformation.Modulation.DISABLED
                    && ClientStateSingleton.Instance.PlayerRadioInfo.control ==
                    PlayerRadioInfo.RadioSwitchControls.HOTAS)
                {
                    var fixedChannels = ClientStateSingleton.Instance.FixedChannels;

                    //now get model
                    if (fixedChannels != null && fixedChannels[radioId - 1] != null)
                    {
                        var radioChannels = fixedChannels[radioId - 1];

                        if (radioChannels.PresetChannels.Count > 0)
                        {
                            int next = currentRadio.channel + 1;

                            if (radioChannels.PresetChannels.Count < next || currentRadio.channel < 1)
                            {
                                //set to first radio
                                SelectRadioChannel(radioChannels.PresetChannels[0], radioId);
                                radioChannels.SelectedPresetChannel = radioChannels.PresetChannels[0];
                            }
                            else
                            {
                                var preset = radioChannels.PresetChannels[next - 1];

                                SelectRadioChannel(preset, radioId);
                                radioChannels.SelectedPresetChannel = preset;
                            }
                        }
                    }
                }
            }
        }

        public static void RadioChannelDown(int radioId)
        {
            var currentRadio = RadioHelper.GetRadio(radioId);

            if (currentRadio != null)
            {
                if (currentRadio.modulation != RadioInformation.Modulation.DISABLED
                    && ClientStateSingleton.Instance.PlayerRadioInfo.control ==
                    PlayerRadioInfo.RadioSwitchControls.HOTAS)
                {
                    var fixedChannels = ClientStateSingleton.Instance.FixedChannels;

                    //now get model
                    if (fixedChannels != null && fixedChannels[radioId - 1] != null)
                    {
                        var radioChannels = fixedChannels[radioId - 1];

                        if (radioChannels.PresetChannels.Count > 0)
                        {
                            int previous = currentRadio.channel - 1;

                            if (previous < 1)
                            {
                                //set to last radio
                                SelectRadioChannel(radioChannels.PresetChannels.Last(), radioId);
                                radioChannels.SelectedPresetChannel = radioChannels.PresetChannels.Last();
                            }
                            else
                            {
                                var preset = radioChannels.PresetChannels[previous - 1];
                                //set to previous radio
                                SelectRadioChannel(preset, radioId);
                                radioChannels.SelectedPresetChannel = preset;
                            }
                        }
                    }
                }
            }
        }

        public static void SetRadioVolume(float volume, int radioId)
        {
            if (volume > 1.0)
            {
                volume = 1.0f;
            }else if (volume < 0)
            {
                volume = 0;
            }

            var currentRadio = RadioHelper.GetRadio(radioId);

            if (currentRadio != null
                && currentRadio.modulation != RadioInformation.Modulation.DISABLED
                && currentRadio.volMode == RadioInformation.VolumeMode.OVERLAY)
            {
                currentRadio.volume = volume;
            }
        }

        public static void ToggleRetransmit(int radioId)
        {
            var radio = GetRadio(radioId);

            if (radio != null)
            {
                if (radio.rtMode == RadioInformation.RetransmitMode.OVERLAY)
                {
                    radio.retransmit = !radio.retransmit;

                    //make radio data stale to force resysnc
                    ClientStateSingleton.Instance.LastSent = 0;
                }
            }

        }
    }
}