using System;
using System.Linq;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings.RadioChannels;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.SRS.Common;
using Ciribob.SRS.Common.PlayerState;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils
{
    public static class RadioHelper
    {
        public static void ToggleGuard(int radioId)
        {
            var radio = GetRadio(radioId);

            if (radio != null)
            {
                if (radio.Config.FrequencyControl == RadioConfig.FreqMode.OVERLAY || radio.Config.GuardFrequencyControl == RadioConfig.FreqMode.OVERLAY)
                {
                    if (radio.SecondaryFrequency > 0)
                    {
                        radio.SecondaryFrequency = 0; // 0 indicates we want it overridden + disabled
                    }
                    else
                    {
                        radio.SecondaryFrequency = 1; //indicates we want it back
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
                if (radio.Config.FrequencyControl == RadioConfig.FreqMode.OVERLAY || radio.Config.GuardFrequencyControl == RadioConfig.FreqMode.OVERLAY)
                {
                    if (!enabled)
                    {
                        radio.SecondaryFrequency = 0; // 0 indicates we want it overridden + disabled
                    }
                    else
                    {
                        radio.SecondaryFrequency = 1; //indicates we want it back
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
                if (radio.Modulation != RadioConfig.Modulation.DISABLED
                    && radio.Modulation != RadioConfig.Modulation.INTERCOM
                    && radio.Config.FrequencyControl == RadioConfig.FreqMode.OVERLAY)
                {
                    if (delta)
                    {
                        radio.Frequency = (int)Math.Round(radio.Frequency + frequency);
                    }
                    else
                    {
                        radio.Frequency = (int)Math.Round(frequency);
                    }

                    //make sure we're not over or under a limit
                    if (radio.Frequency > radio.Config.MaxFrequency)
                    {
                        inLimit = false;
                        radio.Frequency = radio.Config.MaxFrequency;
                    }
                    else if (radio.Frequency < radio.Config.MinimumFrequency)
                    {
                        inLimit = false;
                        radio.Frequency = radio.Config.MinimumFrequency;
                    }

                    //set to no channel
                    radio.CurrentChannel = -1;

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
                if (radio.Modulation != RadioConfig.Modulation.DISABLED
                    && ClientStateSingleton.Instance.PlayerUnitState.control ==
                    PlayerUnitState.RadioSwitchControls.HOTAS)
                {
                    ClientStateSingleton.Instance.PlayerUnitState.SelectedRadio = (short) radioId;
                    return true;
                }
            }

            return false;
        }

        public static Radio GetRadio(int radio)
        {
            var dcsPlayerRadioInfo = ClientStateSingleton.Instance.PlayerUnitState;

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent() &&
                radio < dcsPlayerRadioInfo.Radios.Count && (radio >= 0))
            {
                return dcsPlayerRadioInfo.Radios[radio];
            }

            return null;
        }



        public static void SelectNextRadio()
        {
            var dcsPlayerRadioInfo = ClientStateSingleton.Instance.PlayerUnitState;

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent() &&
                dcsPlayerRadioInfo.control == PlayerUnitState.RadioSwitchControls.HOTAS)
            {
                if (dcsPlayerRadioInfo.SelectedRadio < 0
                    || dcsPlayerRadioInfo.SelectedRadio > dcsPlayerRadioInfo.Radios.Count
                    || dcsPlayerRadioInfo.SelectedRadio + 1 > dcsPlayerRadioInfo.Radios.Count)
                {
                    SelectRadio(1);

                    return;
                }
                else
                {
                    int currentRadio = dcsPlayerRadioInfo.SelectedRadio;

                    //find next radio
                    for (int i = currentRadio + 1; i < dcsPlayerRadioInfo.Radios.Count; i++)
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
            var dcsPlayerRadioInfo = ClientStateSingleton.Instance.PlayerUnitState;

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent() &&
                dcsPlayerRadioInfo.control == PlayerUnitState.RadioSwitchControls.HOTAS)
            {
                if (dcsPlayerRadioInfo.SelectedRadio < 0
                    || dcsPlayerRadioInfo.SelectedRadio > dcsPlayerRadioInfo.Radios.Count)
                {
                    dcsPlayerRadioInfo.SelectedRadio = 1;
                    return;
                }
                else
                {
                    int currentRadio = dcsPlayerRadioInfo.SelectedRadio;

                    //find previous radio
                    for (int i = currentRadio - 1; i > 0; i--)
                    {
                        if (SelectRadio(i))
                        {
                            return;
                        }
                    }

                    //search down to current radio
                    for (int i = dcsPlayerRadioInfo.Radios.Count; i > currentRadio; i--)
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

                if (radio != null) radio.CurrentChannel = selectedPresetChannel.Channel;
            }
        }

        public static void RadioChannelUp(int radioId)
        {
            var currentRadio = RadioHelper.GetRadio(radioId);

            if (currentRadio != null)
            {
                if (currentRadio.Modulation != RadioConfig.Modulation.DISABLED
                    && ClientStateSingleton.Instance.PlayerUnitState.control ==
                    PlayerUnitState.RadioSwitchControls.HOTAS)
                {
                    var fixedChannels = ClientStateSingleton.Instance.FixedChannels;

                    //now get model
                    if (fixedChannels != null && fixedChannels[radioId - 1] != null)
                    {
                        var radioChannels = fixedChannels[radioId - 1];

                        if (radioChannels.PresetChannels.Count > 0)
                        {
                            int next = currentRadio.CurrentChannel + 1;

                            if (radioChannels.PresetChannels.Count < next || currentRadio.CurrentChannel < 1)
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
                if (currentRadio.Modulation != RadioConfig.Modulation.DISABLED
                    && ClientStateSingleton.Instance.PlayerUnitState.control ==
                    PlayerUnitState.RadioSwitchControls.HOTAS)
                {
                    var fixedChannels = ClientStateSingleton.Instance.FixedChannels;

                    //now get model
                    if (fixedChannels != null && fixedChannels[radioId - 1] != null)
                    {
                        var radioChannels = fixedChannels[radioId - 1];

                        if (radioChannels.PresetChannels.Count > 0)
                        {
                            int previous = currentRadio.CurrentChannel - 1;

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
                && currentRadio.Modulation != RadioConfig.Modulation.DISABLED
                && currentRadio.Config.VolumeControl == RadioConfig.VolumeMode.OVERLAY)
            {
                currentRadio.Volume = volume;
            }
        }

        public static void ToggleRetransmit(int radioId)
        {
            var radio = GetRadio(radioId);

            if (radio != null)
            {
                if (radio.Config.RetransmitControl == RadioConfig.RetransmitMode.OVERLAY)
                {
                    radio.Retransmit = !radio.Retransmit;

                    //make radio data stale to force resysnc
                    ClientStateSingleton.Instance.LastSent = 0;
                }
            }

        }
    }
}