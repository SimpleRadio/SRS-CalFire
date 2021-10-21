using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Threading;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Managers;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientList;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network.Client;
using NAudio.CoreAudioApi;
using NLog;
using SharpDX.Multimedia;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly AudioManager _audioManager;

        private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;
        private readonly DispatcherTimer _updateTimer;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private AudioPreview _audioPreview;
        private AwacsRadioOverlayWindow.RadioOverlayWindow _awacsRadioOverlay;

        private ClientListWindow _clientListWindow;
        // private SRSClientSyncHandler _client;

        private Overlay.RadioOverlayWindow _radioOverlayWindow;


        private ServerSettingsWindow _serverSettingsWindow;

        //used to debounce toggle
        private long _toggleShowHide;
        private TCPClientHandler _client;

        public MainWindowViewModel()
        {
            _audioManager = new AudioManager(AudioOutput.WindowsN);

            PreviewCommand = new DelegateCommand(() => PreviewAudio());
                
            ConnectCommand = new DelegateCommand(Connect,()=>true);

            // InitDefaultAddress();


            // _audioManager.SpeakerBoost = VolumeConversionHelper.ConvertVolumeSliderToScale((float)SpeakerBoost.Value);
            //
            // if ((SpeakerBoostLabel != null) && (SpeakerBoost != null))
            // {
            //     SpeakerBoostLabel.Content = VolumeConversionHelper.ConvertLinearDiffToDB(_audioManager.SpeakerBoost);
            // }

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _updateTimer.Tick += UpdatePlayerCountAndVUMeters;
            _updateTimer.Start();
        }

        public DelegateCommand ConnectCommand { get; set; }

        public ClientStateSingleton ClientState { get; } = ClientStateSingleton.Instance;
        public ConnectedClientsSingleton Clients { get; } = ConnectedClientsSingleton.Instance;
        public AudioInputSingleton AudioInput { get; } = AudioInputSingleton.Instance;
        public AudioOutputSingleton AudioOutput { get; } = AudioOutputSingleton.Instance;

        public float SpeakerVU
        {
            get
            {
                if (_audioPreview != null && _audioPreview.IsPreviewing)
                    return _audioPreview.SpeakerMax;
                if (_audioManager != null) return _audioManager.SpeakerMax;

                return -100;
            }
        }

        public float MicVU
        {
            get
            {
                if (_audioPreview != null && _audioPreview.IsPreviewing)
                    return _audioPreview.MicMax;
                if (_audioManager != null) return _audioManager.MicMax;

                return -100;
            }
        }

        public ICommand PreviewCommand { get; set; }

        public string PreviewText
        {
            get
            {
                if (_audioPreview == null || !_audioPreview.IsPreviewing)
                    return "preview audio";
                return "stop preview";
            }
        }

        public double SpeakerBoost
        {
            get
            {
                var boost = _globalSettings.GetClientSetting(GlobalSettingsKeys.SpeakerBoost).DoubleValue;
                _audioManager.SpeakerBoost = VolumeConversionHelper.ConvertVolumeSliderToScale((float)boost);
                if (_audioPreview != null) _audioPreview.SpeakerBoost = _audioManager.SpeakerBoost;
                return boost;
            }
            set
            {
                _globalSettings.SetClientSetting(GlobalSettingsKeys.SpeakerBoost,
                    value.ToString(CultureInfo.InvariantCulture));
                _audioManager.SpeakerBoost = VolumeConversionHelper.ConvertVolumeSliderToScale((float)value);

                if (_audioPreview != null) _audioPreview.SpeakerBoost = _audioManager.SpeakerBoost;
                NotifyPropertyChanged();
                NotifyPropertyChanged("SpeakerBoostText");
            }
        }

        public string SpeakerBoostText =>
            VolumeConversionHelper.ConvertLinearDiffToDB(
                VolumeConversionHelper.ConvertVolumeSliderToScale((float)SpeakerBoost));

        public InputDeviceManager InputManager { get; set; }


        public string ServerAddress
        {
            get
            {
                var savedAddress =  _globalSettings.GetClientSetting(GlobalSettingsKeys.LastServer);

                if (savedAddress == null)
                {
                    return "127.0.0.1:5002";
                }
                else
                {
                    return savedAddress.RawValue;
                }
            }
            set
            {
                if (value != null)
                {
                    _globalSettings.SetClientSetting(GlobalSettingsKeys.LastServer, value);
                    NotifyPropertyChanged();
                }
            }
        }

        public bool AudioSettingsEnabled
        {
            get
            {
                if (_audioPreview != null && _audioPreview.IsPreviewing) return false;

                return true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }


        private void UpdatePlayerCountAndVUMeters(object sender, EventArgs e)
        {
            NotifyPropertyChanged("SpeakerVU");
            NotifyPropertyChanged("MicVU");
            ConnectedClientsSingleton.Instance.NotifyAll();
        }

        private void Stop(bool connectionError = false)
        {
            // if (ClientState.IsConnected && _globalSettings.GetClientSettingBool(GlobalSettingsKeys.PlayConnectionSounds))
            // {
            //     try
            //     {
            //         Sounds.BeepDisconnected.Play();
            //     }
            //     catch (Exception ex)
            //     {
            //         Logger.Warn(ex, "Failed to play disconnect sound");
            //     }
            // }
            //
            // ClientState.IsConnectionErrored = connectionError;
            //
            // StartStop.Content = "Connect";
            // StartStop.IsEnabled = true;
            // Mic.IsEnabled = true;
            // Speakers.IsEnabled = true;
            // MicOutput.IsEnabled = true;
            // Preview.IsEnabled = true;
            // ClientState.IsConnected = false;
            // ToggleServerSettings.IsEnabled = false;
            //
            // ConnectExternalAWACSMode.IsEnabled = false;
            // ConnectExternalAWACSMode.Content = "Connect External AWACS MODE (EAM)";
            //
            // if (!string.IsNullOrWhiteSpace(ClientState.LastSeenName) &&
            //     _globalSettings.GetClientSetting(GlobalSettingsKeys.LastSeenName).StringValue != ClientState.LastSeenName)
            // {
            //     _globalSettings.SetClientSetting(GlobalSettingsKeys.LastSeenName, ClientState.LastSeenName);
            // }
            //
            // try
            // {
            //     _audioManager.StopEncoding();
            // }
            // catch (Exception ex)
            // {
            // }
            //
            // if (_client != null)
            // {
            //     _client.Disconnect();
            //     _client = null;
            // }
            //
            // ClientState.DcsPlayerRadioInfo.Reset();
            // ClientState.PlayerCoaltionLocationMetadata.Reset();
        }

        private void Connect()
        {
            if (ClientState.IsConnected)
            {
                Stop();
            }
            else
            {
                SaveSelectedInputAndOutput();
        
                try
                {
                    //process hostname
                    var resolvedAddresses = Dns.GetHostAddresses(GetAddressFromTextBox());
                    var ip = resolvedAddresses.FirstOrDefault(xa => xa.AddressFamily == AddressFamily.InterNetwork); // Ensure we get an IPv4 address in case the host resolves to both IPv6 and IPv4
        
                    if (ip != null)
                    {
                        var resolvedIp = ip;
                        var port = GetPortFromTextBox();

                        _client = new TCPClientHandler();
                        _client.TryConnect(new IPEndPoint(resolvedIp, port));

                        // _client = new SRSClientSyncHandler(_guid, UpdateUICallback, delegate(string name)
                        // {
                        //     // try
                        //     // {
                        //     //     //on MAIN thread
                        //     //     Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                        //     //         new ThreadStart(() =>
                        //     //         {
                        //     //             //Handle Aircraft Name - find matching profile and select if you can
                        //     //             name = Regex.Replace(name.Trim().ToLower(), "[^a-zA-Z0-9]", "");
                        //     //
                        //     //             foreach (var profileName in _globalSettings.ProfileSettingsStore.ProfileNames)
                        //     //             {
                        //     //                 if (name.StartsWith(Regex.Replace(profileName.Trim().ToLower(), "[^a-zA-Z0-9]",
                        //     //                     "")))
                        //     //                 {
                        //     //                     ControlsProfile.SelectedItem = profileName;
                        //     //                     return;
                        //     //                 }
                        //     //             }
                        //     //
                        //     //             ControlsProfile.SelectedIndex = 0;
                        //     //
                        //     //         }));
                        //     // }
                        //     // catch (Exception ex)
                        //     // {
                        //     // }
                        //
                        // });
                        // _client.TryConnect(new IPEndPoint(_resolvedIp, _port), ConnectCallback);
                        //
                        // StartStop.Content = "Connecting...";
                        // StartStop.IsEnabled = false;
                        // Mic.IsEnabled = false;
                        // Speakers.IsEnabled = false;
                        // MicOutput.IsEnabled = false;

                        if (_audioPreview != null)
                        {
                            _audioPreview.StopEncoding();
                            _audioPreview = null;
                        }
                    }
                    else
                    {
                        //invalid ID
                        MessageBox.Show("Invalid IP or Host Name!", "Host Name Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
        
                        ClientState.IsConnected = false;
                        // ToggleServerSettings.IsEnabled = false;
                    }
                }
                catch (Exception ex) when (ex is SocketException || ex is ArgumentException)
                {
                    MessageBox.Show("Invalid IP or Host Name!", "Host Name Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
        
                    ClientState.IsConnected = false;
                    // ToggleServerSettings.IsEnabled = false;
                }
            }
        }

        private string GetAddressFromTextBox()
        {
            var addr = ServerAddress.Trim();
        
            if (addr.Contains(":"))
            {
                return addr.Split(':')[0];
            }
        
            return addr;
        }
        
        private int GetPortFromTextBox()
        {
            var addr = ServerAddress.Trim();
        
            if (addr.Contains(":"))
            {
                int port;
                if (int.TryParse(addr.Split(':')[1], out port))
                {
                    return port;
                }
                throw new ArgumentException("specified port is  valid");
            }
        
            return 5002;
        }

        // private void Stop(bool connectionError = false)
        // {
        //     if (ClientState.IsConnected && _globalSettings.GetClientSettingBool(GlobalSettingsKeys.PlayConnectionSounds))
        //     {
        //         try
        //         {
        //             Sounds.BeepDisconnected.Play();
        //         }
        //         catch (Exception ex)
        //         {
        //             Logger.Warn(ex, "Failed to play disconnect sound");
        //         }
        //     }
        //
        //     ClientState.IsConnectionErrored = connectionError;
        //
        //     StartStop.Content = "Connect";
        //     StartStop.IsEnabled = true;
        //     Mic.IsEnabled = true;
        //     Speakers.IsEnabled = true;
        //     MicOutput.IsEnabled = true;
        //     Preview.IsEnabled = true;
        //     ClientState.IsConnected = false;
        //     ToggleServerSettings.IsEnabled = false;
        //
        //
        //     if (!string.IsNullOrWhiteSpace(ClientState.LastSeenName) &&
        //         _globalSettings.GetClientSetting(GlobalSettingsKeys.LastSeenName).StringValue != ClientState.LastSeenName)
        //     {
        //         _globalSettings.SetClientSetting(GlobalSettingsKeys.LastSeenName, ClientState.LastSeenName);
        //     }
        //
        //     try
        //     {
        //         _audioManager.StopEncoding();
        //     }
        //     catch (Exception ex)
        //     {
        //     }
        //     //
        //     // if (_client != null)
        //     // {
        //     //     _client.Disconnect();
        //     //     _client = null;
        //     // }
        //
        //     ClientState.PlayerUnitState.Reset();
        //     ClientState.PlayerCoaltionLocationMetadata.Reset();
        // }

        private void SaveSelectedInputAndOutput()
        {
            //save app settings
            // Only save selected microphone if one is actually available, resulting in a crash otherwise
            if (AudioInput.MicrophoneAvailable)
            {
                if (AudioInput.SelectedAudioInput.Value == null)
                {
                    _globalSettings.SetClientSetting(GlobalSettingsKeys.AudioInputDeviceId, "default");
                }
                else
                {
                    var input = ((MMDevice)AudioInput.SelectedAudioInput.Value).ID;
                    _globalSettings.SetClientSetting(GlobalSettingsKeys.AudioInputDeviceId, input);
                }
            }

            if (AudioOutput.SelectedAudioOutput.Value == null)
            {
                _globalSettings.SetClientSetting(GlobalSettingsKeys.AudioOutputDeviceId, "default");
            }
            else
            {
                var output = (MMDevice)AudioOutput.SelectedAudioOutput.Value;
                _globalSettings.SetClientSetting(GlobalSettingsKeys.AudioOutputDeviceId, output.ID);
            }

            //check if we have optional output
            if (AudioOutput.SelectedMicAudioOutput.Value != null)
            {
                var micOutput = (MMDevice)AudioOutput.SelectedMicAudioOutput.Value;
                _globalSettings.SetClientSetting(GlobalSettingsKeys.MicAudioOutputDeviceId, micOutput.ID);
            }
            else
            {
                _globalSettings.SetClientSetting(GlobalSettingsKeys.MicAudioOutputDeviceId, "");
            }

            ShowMicPassthroughWarning();
        }

        private void ShowMicPassthroughWarning()
        {
            if (_globalSettings.GetClientSetting(GlobalSettingsKeys.MicAudioOutputDeviceId).RawValue
                .Equals(_globalSettings.GetClientSetting(GlobalSettingsKeys.AudioOutputDeviceId).RawValue))
                MessageBox.Show(
                    "Mic Output and Speaker Output should not be set to the same device!\n\nMic Output is just for recording and not for use as a sidetone. You will hear yourself with a small delay!\n\nHit disconnect and change Mic Output / Passthrough",
                    "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
        }
        //
        // private void ConnectCallback(bool result, bool connectionError, string connection)
        // {
        //     string currentConnection = ServerIp.Text.Trim();
        //     if (!currentConnection.Contains(":"))
        //     {
        //         currentConnection += ":5002";
        //     }
        //
        //     if (result)
        //     {
        //         if (!ClientState.IsConnected)
        //         {
        //             try
        //             {
        //                 StartStop.Content = "Disconnect";
        //                 StartStop.IsEnabled = true;
        //
        //                 ClientState.IsConnected = true;
        //                 ClientState.IsVoipConnected = false;
        //
        //                 if (_globalSettings.GetClientSettingBool(GlobalSettingsKeys.PlayConnectionSounds))
        //                 {
        //                     try
        //                     {
        //                         Sounds.BeepConnected.Play();
        //                     }
        //                     catch (Exception ex)
        //                     {
        //                         Logger.Warn(ex, "Failed to play connect sound");
        //                     }
        //                 }
        //
        //                 _globalSettings.SetClientSetting(GlobalSettingsKeys.LastServer, ServerIp.Text);
        //
        //                 _audioManager.StartEncoding(_guid, InputManager,
        //                     _resolvedIp, _port);
        //             }
        //             catch (Exception ex)
        //             {
        //                 Logger.Error(ex,
        //                     "Unable to get audio device - likely output device error - Pick another. Error:" +
        //                     ex.Message);
        //                 Stop();
        //
        //                 var messageBoxResult = CustomMessageBox.ShowYesNo(
        //                     "Problem initialising Audio Output!\n\nTry a different Output device and please post your clientlog.txt to the support Discord server.\n\nJoin support Discord server now?",
        //                     "Audio Output Error",
        //                     "OPEN PRIVACY SETTINGS",
        //                     "JOIN DISCORD SERVER",
        //                     MessageBoxImage.Error);
        //
        //                 if (messageBoxResult == MessageBoxResult.Yes) Process.Start("https://discord.gg/baw7g3t");
        //             }
        //         }
        //     }
        //     else if (string.Equals(currentConnection, connection, StringComparison.OrdinalIgnoreCase))
        //     {
        //         // Only stop connection/reset state if connection is currently active
        //         // Autoconnect mismatch will quickly disconnect/reconnect, leading to double-callbacks
        //         Stop(connectionError);
        //     }
        //     else
        //     {
        //         if (!ClientState.IsConnected)
        //         {
        //             Stop(connectionError);
        //         }
        //     }
        // }

        public void OnClosing()
        {
            if (!string.IsNullOrWhiteSpace(ClientState.LastSeenName) &&
                _globalSettings.GetClientSetting(GlobalSettingsKeys.LastSeenName).StringValue !=
                ClientState.LastSeenName)
                _globalSettings.SetClientSetting(GlobalSettingsKeys.LastSeenName, ClientState.LastSeenName);

            //stop timer
            _updateTimer?.Stop();

            // Stop();
            //
            // _audioPreview?.StopEncoding();
            // _audioPreview = null;
            //
            // _radioOverlayWindow?.Close();
            // _radioOverlayWindow = null;
            //
            // _awacsRadioOverlay?.Close();
            // _awacsRadioOverlay = null;
        }

        private void PreviewAudio()
        {
            if (_audioPreview == null)
            {
                if (!AudioInput.MicrophoneAvailable)
                {
                    Logger.Info("Unable to preview audio, no valid audio input device available or selected");
                    return;
                }

                //get device
                try
                {
                    SaveSelectedInputAndOutput();

                    _audioPreview = new AudioPreview();
                    _audioPreview.SpeakerBoost = VolumeConversionHelper.ConvertVolumeSliderToScale((float)SpeakerBoost);
                    _audioPreview.StartPreview(AudioOutput.WindowsN);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex,
                        "Unable to preview audio - likely output device error - Pick another. Error:" + ex.Message);
                }
            }
            else
            {
                _audioPreview.StopEncoding();
                _audioPreview = null;
            }

            NotifyPropertyChanged("PreviewText");
            NotifyPropertyChanged("AudioSettingsEnabled");
        }


        // private void SpeakerBoost_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        // {
        //     var convertedValue = VolumeConversionHelper.ConvertVolumeSliderToScale((float)SpeakerBoost.Value);
        //
        //     if (_audioPreview != null)
        //     {
        //         _audioPreview.SpeakerBoost = convertedValue;
        //     }
        //     if (_audioManager != null)
        //     {
        //         _audioManager.SpeakerBoost = convertedValue;
        //     }
        //
        //     _globalSettings.SetClientSetting(GlobalSettingsKeys.SpeakerBoost,
        //         SpeakerBoost.Value.ToString(CultureInfo.InvariantCulture));
        //
        //
        //     if ((SpeakerBoostLabel != null) && (SpeakerBoost != null))
        //     {
        //         SpeakerBoostLabel.Content = VolumeConversionHelper.ConvertLinearDiffToDB(convertedValue);
        //     }
        // }


        private void ShowOverlay_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleOverlay(true);
        }

        private void ToggleOverlay(bool uiButton)
        {
            //debounce show hide (1 tick = 100ns, 6000000 ticks = 600ms debounce)
            if (DateTime.Now.Ticks - _toggleShowHide > 6000000 || uiButton)
            {
                _toggleShowHide = DateTime.Now.Ticks;
                if (_radioOverlayWindow == null || !_radioOverlayWindow.IsVisible ||
                    _radioOverlayWindow.WindowState == WindowState.Minimized)
                {
                    //hide awacs panel
                    _awacsRadioOverlay?.Close();
                    _awacsRadioOverlay = null;

                    _radioOverlayWindow?.Close();

                    _radioOverlayWindow = new Overlay.RadioOverlayWindow();


                    _radioOverlayWindow.ShowInTaskbar =
                        !_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide);
                    _radioOverlayWindow.Show();
                }
                else
                {
                    _radioOverlayWindow?.Close();
                    _radioOverlayWindow = null;
                }
            }
        }

        private void ShowAwacsOverlay_OnClick(object sender, RoutedEventArgs e)
        {
            if (_awacsRadioOverlay == null || !_awacsRadioOverlay.IsVisible ||
                _awacsRadioOverlay.WindowState == WindowState.Minimized)
            {
                //close normal overlay
                _radioOverlayWindow?.Close();
                _radioOverlayWindow = null;

                _awacsRadioOverlay?.Close();

                _awacsRadioOverlay = new AwacsRadioOverlayWindow.RadioOverlayWindow();
                _awacsRadioOverlay.ShowInTaskbar =
                    !_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide);
                _awacsRadioOverlay.Show();
            }
            else
            {
                _awacsRadioOverlay?.Close();
                _awacsRadioOverlay = null;
            }
        }

        private void ToggleServerSettings_OnClick(object sender, RoutedEventArgs e)
        {
            if (_serverSettingsWindow == null || !_serverSettingsWindow.IsVisible ||
                _serverSettingsWindow.WindowState == WindowState.Minimized)
            {
                _serverSettingsWindow?.Close();

                _serverSettingsWindow = new ServerSettingsWindow();
                _serverSettingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _serverSettingsWindow.Owner = Application.Current.MainWindow;
                _serverSettingsWindow.Show();
            }
            else
            {
                _serverSettingsWindow?.Close();
                _serverSettingsWindow = null;
            }
        }

        private void ShowClientList_OnClick(object sender, RoutedEventArgs e)
        {
            if (_clientListWindow == null || !_clientListWindow.IsVisible ||
                _clientListWindow.WindowState == WindowState.Minimized)
            {
                _clientListWindow?.Close();

                _clientListWindow = new ClientListWindow();
                _clientListWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _clientListWindow.Owner = Application.Current.MainWindow;
                _clientListWindow.Show();
            }
            else
            {
                _clientListWindow?.Close();
                _clientListWindow = null;
            }
        }
    }
}