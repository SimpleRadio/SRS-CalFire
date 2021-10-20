using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Managers;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
using Microsoft.Win32;
using NLog;
using InputBinding = Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings.InputBinding;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl
{
    public class ClientSettingsViewModel: INotifyPropertyChanged
    {
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand ResetOverlayCommand { get; set; }

        public ICommand CreateProfileCommand { get; set; }
        public ICommand CopyProfileCommand { get; set; }
        public ICommand RenameProfileCommand { get; set; }
        public ICommand DeleteProfileCommand { get; set; }

        public void NotifyPropertyChanged([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        public ClientSettingsViewModel()
        {
            ResetOverlayCommand = new DelegateCommand(() =>
            {
                //TODO trigger event on messagehub
                //close overlay
                //    _radioOverlayWindow?.Close();
                //    _radioOverlayWindow = null;

                _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioX, 300);
                _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioY, 300);
                _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioWidth, 122);
                _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioHeight, 270);
                _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioOpacity, 1.0);
            });

            CreateProfileCommand = new DelegateCommand(() =>
            {
                var inputProfileWindow = new InputProfileWindow.InputProfileWindow(name =>
                {
                    if (name.Trim().Length > 0)
                    {
                        _globalSettings.ProfileSettingsStore.AddNewProfile(name);

                        NotifyPropertyChanged("AvailableProfiles");
                        ReloadSettings();
                    }
                });
                inputProfileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                inputProfileWindow.Owner = Application.Current.MainWindow;
                inputProfileWindow.ShowDialog();
            });

            CopyProfileCommand = new DelegateCommand(() =>
            {
                var current = _globalSettings.ProfileSettingsStore.CurrentProfileName;
                var inputProfileWindow = new InputProfileWindow.InputProfileWindow(name =>
                {
                    if (name.Trim().Length > 0)
                    {
                        _globalSettings.ProfileSettingsStore.CopyProfile(current, name);
                        NotifyPropertyChanged("AvailableProfiles");
                        ReloadSettings();
                    }
                });
                inputProfileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                inputProfileWindow.Owner = Application.Current.MainWindow;
                inputProfileWindow.ShowDialog();
                
            });

            RenameProfileCommand = new DelegateCommand(() =>
            {
                var current = _globalSettings.ProfileSettingsStore.CurrentProfileName;
                if (current.Equals("default"))
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "Cannot rename the default input!",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    var oldName = current;
                    var inputProfileWindow = new InputProfileWindow.InputProfileWindow(name =>
                    {
                        if (name.Trim().Length > 0)
                        {
                            _globalSettings.ProfileSettingsStore.RenameProfile(oldName, name);
                            SelectedProfile = _globalSettings.ProfileSettingsStore.CurrentProfileName;
                            NotifyPropertyChanged("AvailableProfiles");
                            ReloadSettings();
                        }
                    }, true, oldName);
                    inputProfileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    inputProfileWindow.Owner = Application.Current.MainWindow;
                    inputProfileWindow.ShowDialog();
                }

            });

            DeleteProfileCommand = new DelegateCommand(() =>
            {
                var current = _globalSettings.ProfileSettingsStore.CurrentProfileName;
                
                if (current.Equals("default"))
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "Cannot delete the default input!",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    var result = MessageBox.Show(Application.Current.MainWindow,
                        $"Are you sure you want to delete {current} ?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                
                    if (result == MessageBoxResult.Yes)
                    { 
                        _globalSettings.ProfileSettingsStore.RemoveProfile(current);

                      
                        SelectedProfile = _globalSettings.ProfileSettingsStore.CurrentProfileName;
                        NotifyPropertyChanged("AvailableProfiles");
                        ReloadSettings();
                    }
                
                }
            });

        }

        private void ReloadSettings()
        {
            NotifyPropertyChanged("MinimiseToTray");
            NotifyPropertyChanged("StartMinimised");
            NotifyPropertyChanged("MicAGC");
            NotifyPropertyChanged("MicDenoise");
            NotifyPropertyChanged("PlayConnectionSounds");
            NotifyPropertyChanged("RadioSwitchIsPTT");
            NotifyPropertyChanged("AutoSelectChannel");
            NotifyPropertyChanged("PTTReleaseDelay");
            NotifyPropertyChanged("PTTStartDelay");
            NotifyPropertyChanged("HotIntercomMicToggle");
            NotifyPropertyChanged("RadioRxStartToggle");
            NotifyPropertyChanged("RadioRxEndToggle");
            NotifyPropertyChanged("RadioTxStartToggle");
            NotifyPropertyChanged("RadioTxEndToggle");
            NotifyPropertyChanged("SelectedRadioTransmissionStartEffect");
            NotifyPropertyChanged("SelectedRadioTransmissionEndEffect");
            NotifyPropertyChanged("RadioSoundEffectsToggle");
            NotifyPropertyChanged("RadioEffectsClippingToggle");
            NotifyPropertyChanged("FMRadioToneToggle");
            NotifyPropertyChanged("FMRadioToneVolume");
            NotifyPropertyChanged("BackgroundRadioNoiseToggle");
            NotifyPropertyChanged("UHFEffectVolume");
            NotifyPropertyChanged("VHFEffectVolume");
            NotifyPropertyChanged("HFEffectVolume");
            NotifyPropertyChanged("FMEffectVolume");
            NotifyPropertyChanged("RadioChannel1");
            NotifyPropertyChanged("RadioChannel2");
            NotifyPropertyChanged("RadioChannel3");
            NotifyPropertyChanged("RadioChannel4");
            NotifyPropertyChanged("RadioChannel5");
            NotifyPropertyChanged("RadioChannel6");
            NotifyPropertyChanged("RadioChannel7");
            NotifyPropertyChanged("RadioChannel8");
            NotifyPropertyChanged("RadioChannel9");
            NotifyPropertyChanged("RadioChannel10");
            NotifyPropertyChanged("Intercom");
            NotifyPropertyChanged("SelectedProfile");
            
        }

        /**
         * Global Settings
         */
        public bool HideRadioOverlayTaskbar
        {
            get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide);
            set
            {
                _globalSettings.SetClientSetting(GlobalSettingsKeys.RadioOverlayTaskbarHide, value);
                
                //TODO trigger event on messagehub
                NotifyPropertyChanged();

                // if (_radioOverlayWindow != null)
                //     _radioOverlayWindow.ShowInTaskbar = !_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide);
                // else if (_awacsRadioOverlay != null) _awacsRadioOverlay.ShowInTaskbar = !_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide);
            }
        }

        public bool AllowMoreInputs
        {
            get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.ExpandControls);
            set
            {
                _globalSettings.SetClientSetting(GlobalSettingsKeys.ExpandControls, value);
                NotifyPropertyChanged();
                MessageBox.Show(
                "You must restart SRS for this setting to take effect.\n\nTurning this on will allow almost any DirectX device to be used as input expect a Mouse but may cause issues with other devices being detected. \n\nUse device white listing instead",
                "Restart SRS", MessageBoxButton.OK,
                MessageBoxImage.Warning);
                
            }
        }

        public bool MinimiseToTray
        {
            get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.MinimiseToTray);
            set
            {
              
                _globalSettings.SetClientSetting(GlobalSettingsKeys.MinimiseToTray, value);
                NotifyPropertyChanged();
            }
        }

        public bool StartMinimised
        {
            get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.StartMinimised);
            set
            {
               
                _globalSettings.SetClientSetting(GlobalSettingsKeys.StartMinimised, value);
                NotifyPropertyChanged();
            }
        }

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

        public bool PlayConnectionSounds
        {
            get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.PlayConnectionSounds);
            set
            {
                _globalSettings.SetClientSetting(GlobalSettingsKeys.PlayConnectionSounds, value);
                NotifyPropertyChanged();
            }
        }

        /**
         * Profile Settings
         */

        public bool RadioSwitchIsPTT
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioSwitchIsPTT);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioSwitchIsPTT,value);
                NotifyPropertyChanged();
            }
        }

        public bool AutoSelectChannel
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.AutoSelectPresetChannel);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.AutoSelectPresetChannel, value);
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

        public bool HotIntercomMicToggle
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.HotIntercomMic);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.HotIntercomMic, value);
                NotifyPropertyChanged();
            }
        }

        public bool RadioRxStartToggle
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_Start);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_Start, value);
                NotifyPropertyChanged();
            }
        }

        public bool RadioRxEndToggle
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End, value);
                NotifyPropertyChanged();
            }
        }

        public bool RadioTxStartToggle
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_Start);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_Start, value);
                NotifyPropertyChanged();
            }
        }

        public bool RadioTxEndToggle
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End, value);
                NotifyPropertyChanged();
            }
        }

        public List<CachedAudioEffect> RadioTransmissionStart
        {
            get
            {
                return CachedAudioEffectProvider.Instance.RadioTransmissionStart;
            }
        }

        public CachedAudioEffect SelectedRadioTransmissionStartEffect
        {
            set
            {
                GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(
                    ProfileSettingsKeys.RadioTransmissionStartSelection, ((CachedAudioEffect)value).FileName);
                NotifyPropertyChanged();
            }
            get
            {
                return CachedAudioEffectProvider.Instance.SelectedRadioTransmissionStartEffect;
            }
        }

        public List<CachedAudioEffect> RadioTransmissionEnd
        {
            get
            {
                return CachedAudioEffectProvider.Instance.RadioTransmissionEnd;
            }
        }

        public CachedAudioEffect SelectedRadioTransmissionEndEffect
        {
            set
            {
                GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(
                    ProfileSettingsKeys.RadioTransmissionEndSelection, ((CachedAudioEffect)value).FileName);
                NotifyPropertyChanged();
            }
            get
            {
                return CachedAudioEffectProvider.Instance.SelectedRadioTransmissionEndEffect;
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
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioEffectsClipping, value);
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
            get => ( _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume)
                    / double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.NATOToneVolume.ToString()], CultureInfo.InvariantCulture)) *100;
            set
            {
                var orig = double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.NATOToneVolume.ToString()], CultureInfo.InvariantCulture);
                var vol = orig * (value / 100);

                _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume,(float) vol);
                NotifyPropertyChanged();
            }
        }

        public bool BackgroundRadioNoiseToggle
        {
            get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioBackgroundNoiseEffect);
            set
            {
                _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioBackgroundNoiseEffect, value);
                NotifyPropertyChanged();
            }
        }

        public double UHFEffectVolume
        {
            get => (_globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume)
                    / double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.UHFNoiseVolume.ToString()], CultureInfo.InvariantCulture)) * 100;
            set
            {
                var orig = double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.UHFNoiseVolume.ToString()], CultureInfo.InvariantCulture);
                var vol = orig * (value / 100);

                _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume, (float)vol);
                NotifyPropertyChanged();
            }
        }

        public double VHFEffectVolume
        {
            get => (_globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume)
                    / double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.VHFNoiseVolume.ToString()], CultureInfo.InvariantCulture)) * 100;
            set
            {
                var orig = double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.VHFNoiseVolume.ToString()], CultureInfo.InvariantCulture);
                var vol = orig * (value / 100);

                _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume, (float)vol);
                NotifyPropertyChanged();
            }
        }

        public double HFEffectVolume
        {
            get => (_globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume)
                    / double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HFNoiseVolume.ToString()], CultureInfo.InvariantCulture)) * 100;
            set
            {
                var orig = double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HFNoiseVolume.ToString()], CultureInfo.InvariantCulture);
                var vol = orig * (value / 100);

                _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume, (float)vol);
                NotifyPropertyChanged();
            }
        }

        public double FMEffectVolume
        {
            get => (_globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume)
                    / double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.FMNoiseVolume.ToString()], CultureInfo.InvariantCulture)) * 100;
            set
            {
                var orig = double.Parse(ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.FMNoiseVolume.ToString()], CultureInfo.InvariantCulture);
                var vol = orig * (value / 100);

                _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume, (float)vol);
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

        public string SelectedProfile
        {
            set
            {
                if (value != null)
                {
                    _globalSettings.ProfileSettingsStore.CurrentProfileName = value;
                    //TODO send event notifying of change to current profile
                    ReloadSettings();
                }

                NotifyPropertyChanged();
            }
            get
            {
                return _globalSettings.ProfileSettingsStore.CurrentProfileName;
            }
        }

        public List<string> AvailableProfiles
        {
            set
            {
                //do nothing
            }
            get
            {
                return _globalSettings.ProfileSettingsStore.ProfileNames;
            }
        }

      
        //
        // private void RenameProfile(object sender, RoutedEventArgs e)
        // {
        //
        //     var current = ControlsProfile.SelectedValue as string;
        //     if (current.Equals("default"))
        //     {
        //         MessageBox.Show(this,
        //             "Cannot rename the default input!",
        //             "Error",
        //             MessageBoxButton.OK,
        //             MessageBoxImage.Error);
        //     }
        //     else
        //     {
        //         var oldName = current;
        //         var inputProfileWindow = new InputProfileWindow.InputProfileWindow(name =>
        //         {
        //             if (name.Trim().Length > 0)
        //             {
        //                 _globalSettings.ProfileSettingsStore.RenameProfile(oldName, name);
        //                 InitSettingsProfiles();
        //             }
        //         }, true, oldName);
        //         inputProfileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //         inputProfileWindow.Owner = this;
        //         inputProfileWindow.ShowDialog();
        //     }
        //
        // }
        //
        //
 
        //
        //
        // private void RescanInputDevices(object sender, RoutedEventArgs e)
        // {
        //     InputManager.InitDevices();
        //     MessageBox.Show(this,
        //         "Input Devices Rescanned",
        //         "New input devices can now be used.",
        //         MessageBoxButton.OK,
        //         MessageBoxImage.Information);
        // }
        //
   
        //

        
        //
        //
        // private void ReloadInputBindings()
        // {
        //     Radio1.LoadInputSettings();
        //     Radio2.LoadInputSettings();
        //     Radio3.LoadInputSettings();
        //     PTT.LoadInputSettings();
        //     Intercom.LoadInputSettings();
        //     RadioOverlay.LoadInputSettings();
        //     Radio4.LoadInputSettings();
        //     Radio5.LoadInputSettings();
        //     Radio6.LoadInputSettings();
        //     Radio7.LoadInputSettings();
        //     Radio8.LoadInputSettings();
        //     Radio9.LoadInputSettings();
        //     Radio10.LoadInputSettings();
        //     Up100.LoadInputSettings();
        //     Up10.LoadInputSettings();
        //     Up1.LoadInputSettings();
        //     Up01.LoadInputSettings();
        //     Up001.LoadInputSettings();
        //     Up0001.LoadInputSettings();
        //     Down100.LoadInputSettings();
        //     Down10.LoadInputSettings();
        //     Down1.LoadInputSettings();
        //     Down01.LoadInputSettings();
        //     Down001.LoadInputSettings();
        //     Down0001.LoadInputSettings();
        //     ToggleGuard.LoadInputSettings();
        //     NextRadio.LoadInputSettings();
        //     PreviousRadio.LoadInputSettings();
        //     ToggleEncryption.LoadInputSettings();
        //     EncryptionKeyIncrease.LoadInputSettings();
        //     EncryptionKeyDecrease.LoadInputSettings();
        //     RadioChannelUp.LoadInputSettings();
        //     RadioChannelDown.LoadInputSettings();
        // }
        //
        //
        // private void InitInput()
        // {
        //     InputManager = new InputDeviceManager(this, ToggleOverlay);
        //
        //     InitSettingsProfiles();
        //
        //     ControlsProfile.SelectionChanged += OnProfileDropDownChanged;
        //
        //     RadioStartTransmitEffect.SelectionChanged += OnRadioStartTransmitEffectChanged;
        //     RadioEndTransmitEffect.SelectionChanged += OnRadioEndTransmitEffectChanged;
        //
        //     Radio1.InputName = "Radio 1";
        //     Radio1.ControlInputBinding = InputBinding.Switch1;
        //     Radio1.InputDeviceManager = InputManager;
        //
        //     Radio2.InputName = "Radio 2";
        //     Radio2.ControlInputBinding = InputBinding.Switch2;
        //     Radio2.InputDeviceManager = InputManager;
        //
        //     Radio3.InputName = "Radio 3";
        //     Radio3.ControlInputBinding = InputBinding.Switch3;
        //     Radio3.InputDeviceManager = InputManager;
        //
        //     PTT.InputName = "Push To Talk - PTT";
        //     PTT.ControlInputBinding = InputBinding.Ptt;
        //     PTT.InputDeviceManager = InputManager;
        //
        //     Intercom.InputName = "Intercom Select";
        //     Intercom.ControlInputBinding = InputBinding.Intercom;
        //     Intercom.InputDeviceManager = InputManager;
        //
        //     RadioOverlay.InputName = "Overlay Toggle";
        //     RadioOverlay.ControlInputBinding = InputBinding.OverlayToggle;
        //     RadioOverlay.InputDeviceManager = InputManager;
        //
        //     Radio4.InputName = "Radio 4";
        //     Radio4.ControlInputBinding = InputBinding.Switch4;
        //     Radio4.InputDeviceManager = InputManager;
        //
        //     Radio5.InputName = "Radio 5";
        //     Radio5.ControlInputBinding = InputBinding.Switch5;
        //     Radio5.InputDeviceManager = InputManager;
        //
        //     Radio6.InputName = "Radio 6";
        //     Radio6.ControlInputBinding = InputBinding.Switch6;
        //     Radio6.InputDeviceManager = InputManager;
        //
        //     Radio7.InputName = "Radio 7";
        //     Radio7.ControlInputBinding = InputBinding.Switch7;
        //     Radio7.InputDeviceManager = InputManager;
        //
        //     Radio8.InputName = "Radio 8";
        //     Radio8.ControlInputBinding = InputBinding.Switch8;
        //     Radio8.InputDeviceManager = InputManager;
        //
        //     Radio9.InputName = "Radio 9";
        //     Radio9.ControlInputBinding = InputBinding.Switch9;
        //     Radio9.InputDeviceManager = InputManager;
        //
        //     Radio10.InputName = "Radio 10";
        //     Radio10.ControlInputBinding = InputBinding.Switch10;
        //     Radio10.InputDeviceManager = InputManager;
        //
        //     Up100.InputName = "Up 100MHz";
        //     Up100.ControlInputBinding = InputBinding.Up100;
        //     Up100.InputDeviceManager = InputManager;
        //
        //     Up10.InputName = "Up 10MHz";
        //     Up10.ControlInputBinding = InputBinding.Up10;
        //     Up10.InputDeviceManager = InputManager;
        //
        //     Up1.InputName = "Up 1MHz";
        //     Up1.ControlInputBinding = InputBinding.Up1;
        //     Up1.InputDeviceManager = InputManager;
        //
        //     Up01.InputName = "Up 0.1MHz";
        //     Up01.ControlInputBinding = InputBinding.Up01;
        //     Up01.InputDeviceManager = InputManager;
        //
        //     Up001.InputName = "Up 0.01MHz";
        //     Up001.ControlInputBinding = InputBinding.Up001;
        //     Up001.InputDeviceManager = InputManager;
        //
        //     Up0001.InputName = "Up 0.001MHz";
        //     Up0001.ControlInputBinding = InputBinding.Up0001;
        //     Up0001.InputDeviceManager = InputManager;
        //
        //
        //     Down100.InputName = "Down 100MHz";
        //     Down100.ControlInputBinding = InputBinding.Down100;
        //     Down100.InputDeviceManager = InputManager;
        //
        //     Down10.InputName = "Down 10MHz";
        //     Down10.ControlInputBinding = InputBinding.Down10;
        //     Down10.InputDeviceManager = InputManager;
        //
        //     Down1.InputName = "Down 1MHz";
        //     Down1.ControlInputBinding = InputBinding.Down1;
        //     Down1.InputDeviceManager = InputManager;
        //
        //     Down01.InputName = "Down 0.1MHz";
        //     Down01.ControlInputBinding = InputBinding.Down01;
        //     Down01.InputDeviceManager = InputManager;
        //
        //     Down001.InputName = "Down 0.01MHz";
        //     Down001.ControlInputBinding = InputBinding.Down001;
        //     Down001.InputDeviceManager = InputManager;
        //
        //     Down0001.InputName = "Down 0.001MHz";
        //     Down0001.ControlInputBinding = InputBinding.Down0001;
        //     Down0001.InputDeviceManager = InputManager;
        //
        //     ToggleGuard.InputName = "Toggle Guard";
        //     ToggleGuard.ControlInputBinding = InputBinding.ToggleGuard;
        //     ToggleGuard.InputDeviceManager = InputManager;
        //
        //     NextRadio.InputName = "Select Next Radio";
        //     NextRadio.ControlInputBinding = InputBinding.NextRadio;
        //     NextRadio.InputDeviceManager = InputManager;
        //
        //     PreviousRadio.InputName = "Select Previous Radio";
        //     PreviousRadio.ControlInputBinding = InputBinding.PreviousRadio;
        //     PreviousRadio.InputDeviceManager = InputManager;
        //
        //     ToggleEncryption.InputName = "Toggle Encryption";
        //     ToggleEncryption.ControlInputBinding = InputBinding.ToggleEncryption;
        //     ToggleEncryption.InputDeviceManager = InputManager;
        //
        //     EncryptionKeyIncrease.InputName = "Encryption Key Up";
        //     EncryptionKeyIncrease.ControlInputBinding = InputBinding.EncryptionKeyIncrease;
        //     EncryptionKeyIncrease.InputDeviceManager = InputManager;
        //
        //     EncryptionKeyDecrease.InputName = "Encryption Key Down";
        //     EncryptionKeyDecrease.ControlInputBinding = InputBinding.EncryptionKeyDecrease;
        //     EncryptionKeyDecrease.InputDeviceManager = InputManager;
        //
        //     RadioChannelUp.InputName = "Radio Channel Up";
        //     RadioChannelUp.ControlInputBinding = InputBinding.RadioChannelUp;
        //     RadioChannelUp.InputDeviceManager = InputManager;
        //
        //     RadioChannelDown.InputName = "Radio Channel Down";
        //     RadioChannelDown.ControlInputBinding = InputBinding.RadioChannelDown;
        //     RadioChannelDown.InputDeviceManager = InputManager;
        //
        //     TransponderIDENT.InputName = "Transponder IDENT Toggle";
        //     TransponderIDENT.ControlInputBinding = InputBinding.TransponderIDENT;
        //     TransponderIDENT.InputDeviceManager = InputManager;
        // }
      
        // private void OnRadioStartTransmitEffectChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     if (RadioStartTransmitEffect.IsEnabled)
        //     {
        //         GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(ProfileSettingsKeys.RadioTransmissionStartSelection, ((CachedAudioEffect)RadioStartTransmitEffect.SelectedItem).FileName);
        //     }
        // }
        //
        // private void OnRadioEndTransmitEffectChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     if (RadioEndTransmitEffect.IsEnabled)
        //     {
        //         GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(ProfileSettingsKeys.RadioTransmissionEndSelection, ((CachedAudioEffect)RadioEndTransmitEffect.SelectedItem).FileName);
        //     }
        // }
        //
        // void ReloadProfile()
        // {
        //     //switch profiles
        //     Logger.Info(ControlsProfile.SelectedValue as string + " - Profile now in use");
        //     _globalSettings.ProfileSettingsStore.CurrentProfileName = ControlsProfile.SelectedValue as string;
        //
        //     //redraw UI
        //     ReloadInputBindings();
        //     ReloadProfileSettings();
        //     ReloadRadioAudioChannelSettings();
        //
        //     CurrentProfile.Content = _globalSettings.ProfileSettingsStore.CurrentProfileName;
        // }
        //
        //

        // private void InitSettingsProfiles()
        // {
        //     ControlsProfile.IsEnabled = false;
        //     ControlsProfile.Items.Clear();
        //     foreach (var profile in _globalSettings.ProfileSettingsStore.InputProfiles.Keys)
        //     {
        //         ControlsProfile.Items.Add(profile);
        //     }
        //     ControlsProfile.IsEnabled = true;
        //     ControlsProfile.SelectedIndex = 0;
        //
        //     CurrentProfile.Content = _globalSettings.ProfileSettingsStore.CurrentProfileName;
        //
        // }


    }
}
