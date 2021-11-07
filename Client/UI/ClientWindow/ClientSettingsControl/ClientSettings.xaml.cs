using System.Windows.Controls;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl
{
    /// <summary>
    ///     Interaction logic for ClientSettings.xaml
    /// </summary>
    public partial class ClientSettings : UserControl
    {
        public ClientSettings()
        {
            InitializeComponent();
            DataContext = new ClientSettingsViewModel();
        }



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

        // void ReloadProfile()
        // {
        //     //switch profiles
        //     Logger.Info(ControlsProfile.SelectedValue as string + " - Profile now in use));
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