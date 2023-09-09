using System.Globalization;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Models;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Singleton;
using Ciribob.SRS.Common.Helpers;
using NLog;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.Settings;

public class ClientSettingsViewModel : PropertyChangedBase
{
    private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ClientSettingsViewModel()
    {
  
        
    }
    

    /**
         * Global Settings
         */

    
    public bool MicAGC
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AGC);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AGC, value);
            NotifyPropertyChanged();
        }
    }

    public bool MicDenoise
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.Denoise);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.Denoise, value);
            NotifyPropertyChanged();
        }
    }

    
    public bool AutoSelectChannel
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(
            ProfileSettingsKeys.AutoSelectPresetChannel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.AutoSelectPresetChannel,
                value);
            NotifyPropertyChanged();
        }
    }

    public float PTTReleaseDelay
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.PTTReleaseDelay);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.PTTReleaseDelay, value);
            NotifyPropertyChanged();
        }
    }

    public float PTTStartDelay
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.PTTStartDelay);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.PTTStartDelay, value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioRxStartToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_Start);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_Start,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioRxEndToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioTxStartToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_Start);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_Start,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioTxEndToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End,
                value);
            NotifyPropertyChanged();
        }
    }
    
    public bool RadioSoundEffectsToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioEffects);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioEffects, value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioEffectsClippingToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioEffectsClipping);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioEffectsClipping,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool FMRadioToneToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.NATOTone);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.NATOTone, value);
            NotifyPropertyChanged();
        }
    }

    public double FMRadioToneVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.NATOToneVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.NATOToneVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public bool BackgroundRadioNoiseToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys
            .RadioBackgroundNoiseEffect);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(
                ProfileSettingsKeys.RadioBackgroundNoiseEffect, value);
            NotifyPropertyChanged();
        }
    }

    public double UHFEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.UHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.UHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double VHFEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.VHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.VHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double HFEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double FMEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.FMNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.FMNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double GroundEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.GroundNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.GroundNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.GroundNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.GroundNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double AircraftEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.AircraftNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.AircraftNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.AircraftNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.AircraftNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    /**
         * Radio Audio Balance
         */

    public float RadioChannel1
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio1Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio1Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel2
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio2Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio2Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel3
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio3Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio3Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel4
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio4Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio4Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel5
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio5Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio5Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel6
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio6Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio6Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel7
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio7Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio7Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel8
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio8Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio8Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel9
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio9Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio9Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel10
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio10Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio10Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float Intercom
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.IntercomChannel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.IntercomChannel, value);
            NotifyPropertyChanged();
        }
    }
    

    public string PlayerName
    {
        set
        {
            if (value != null) ClientStateSingleton.Instance.PlayerUnitState.Name = value;
        }
        get => ClientStateSingleton.Instance.PlayerUnitState.Name;
    }

    public uint PlayerID
    {
        set
        {
            if (value != null) ClientStateSingleton.Instance.PlayerUnitState.UnitId = value;
        }
        get => ClientStateSingleton.Instance.PlayerUnitState.UnitId;
    }
    
    
    public bool VolumeUpAsPTTToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys
            .VolumeUpAsPTT);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(
                ProfileSettingsKeys.VolumeUpAsPTT, value);
            NotifyPropertyChanged();
        }
    }
}