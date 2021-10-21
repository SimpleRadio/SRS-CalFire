using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.Common.PresetChannels;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
using Ciribob.SRS.Common;
using Ciribob.SRS.Common.PlayerState;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.AwacsRadioOverlayWindow
{
    /// <summary>
    ///     Interaction logic for RadioControlGroup.xaml
    /// </summary>
    public partial class RadioControlGroup : UserControl
    {
        private const double MHz = 1000000;
        private const int MaxSimultaneousTransmissions = 3;
        private bool _dragging;
        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;
        private readonly ConnectedClientsSingleton _connectClientsSingleton = ConnectedClientsSingleton.Instance;

        public PresetChannelsViewModel ChannelViewModel { get; set; }


        public RadioControlGroup()
        {
            DataContext = this; // set data context

            InitializeComponent();

            RadioFrequency.MaxLines = 1;
            RadioFrequency.MaxLength = 7;

            RadioFrequency.LostFocus += RadioFrequencyOnLostFocus;

            RadioFrequency.KeyDown += RadioFrequencyOnKeyDown;

            RadioFrequency.GotFocus += RadioFrequencyOnGotFocus;
        }

        private int _radioId;

        public int RadioId
        {
            private get { return _radioId; }
            set
            {
                _radioId = value;
                UpdateBinding();
            }
        }

        //updates the binding so the changes are picked up for the linked FixedChannelsModel
        private void UpdateBinding()
        {
            ChannelViewModel = _clientStateSingleton.FixedChannels[_radioId - 1];

            var bindingExpression = PresetChannelsView.GetBindingExpression(DataContextProperty);
            bindingExpression?.UpdateTarget();
        }

        private void RadioFrequencyOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            var playerRadioInfo = _clientStateSingleton.PlayerUnitState;

            if (playerRadioInfo == null || !playerRadioInfo.IsCurrent() ||
                RadioId > playerRadioInfo.Radios.Count - 1 || RadioId < 0)
            {
                //remove focus to somewhere else
                RadioVolume.Focus();
                Keyboard.ClearFocus(); //then clear altogether
            }
        }

        private
            void RadioFrequencyOnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Enter)
            {
                //remove focus to somewhere else
                RadioVolume.Focus();
                Keyboard.ClearFocus(); //then clear altogher
            }
        }

        private void RadioFrequencyOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            double freq = 0;
            // Some locales/cultures (e.g. German) do not parse "." as decimal points since they use decimal commas ("123,45"), leading to "123.45" being parsed as "12345" and frequencies being set too high
            // Using an invariant culture makes sure the decimal point is parsed properly for all locales - replacing any commas makes sure people entering numbers in a weird format still get correct results
            if (double.TryParse(RadioFrequency.Text.Replace(',', '.').Trim(), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out freq))
                RadioHelper.UpdateRadioFrequency(freq, RadioId, false);
            else
                RadioFrequency.Text = "";
        }


        private void Up0001_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(0.001, RadioId);
        }

        private void Up001_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(0.01, RadioId);
        }

        private void Up01_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(0.1, RadioId);
        }

        private void Up1_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(1, RadioId);
        }

        private void Up10_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(10, RadioId);
        }

        private void Down10_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(-10, RadioId);
        }

        private void Down1_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(-1, RadioId);
        }

        private void Down01_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(-0.1, RadioId);
        }

        private void Down001_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(-0.01, RadioId);
        }

        private void Down0001_Click(object sender, RoutedEventArgs e)
        {
            RadioHelper.UpdateRadioFrequency(-0.001, RadioId);
        }


        private void RadioSelectSwitch(object sender, RoutedEventArgs e)
        {
            RadioHelper.SelectRadio(RadioId);
        }

        private void RadioFrequencyText_Click(object sender, MouseButtonEventArgs e)
        {
            RadioHelper.SelectRadio(RadioId);
        }

        private void RadioFrequencyText_RightClick(object sender, MouseButtonEventArgs e)
        {
            RadioHelper.ToggleGuard(RadioId);
        }

        private void RadioVolume_DragStarted(object sender, RoutedEventArgs e)
        {
            _dragging = true;
        }


        private void RadioVolume_DragCompleted(object sender, RoutedEventArgs e)
        {
            var currentRadio = (Radio) _clientStateSingleton.PlayerUnitState.Radios[RadioId];

            if (currentRadio.Config.VolumeControl == RadioConfig.VolumeMode.OVERLAY)
            {
                currentRadio.Volume = (float)RadioVolume.Value / 100.0f;
            }

            _dragging = false;
        }

        private void ToggleButtons(bool enable)
        {
            // if (enable)
            // {
            //     Up10.Visibility = Visibility.Visible;
            //     Up1.Visibility = Visibility.Visible;
            //     Up01.Visibility = Visibility.Visible;
            //     Up001.Visibility = Visibility.Visible;
            //     Up0001.Visibility = Visibility.Visible;
            //
            //     Down10.Visibility = Visibility.Visible;
            //     Down1.Visibility = Visibility.Visible;
            //     Down01.Visibility = Visibility.Visible;
            //     Down001.Visibility = Visibility.Visible;
            //     Down0001.Visibility = Visibility.Visible;
            //
            //     Up10.IsEnabled = true;
            //     Up1.IsEnabled = true;
            //     Up01.IsEnabled = true;
            //     Up001.IsEnabled = true;
            //     Up0001.IsEnabled = true;
            //
            //     Down10.IsEnabled = true;
            //     Down1.IsEnabled = true;
            //     Down01.IsEnabled = true;
            //     Down001.IsEnabled = true;
            //     Down0001.IsEnabled = true;
            //
            //     PresetChannelsView.IsEnabled = true;
            //
            //     ChannelTab.Visibility = Visibility.Visible;
            //
            //     if (_clientStateSingleton.PlayerUnitState.simultaneousTransmissionControl ==
            //         PlayerUnitState.SimultaneousTransmissionControl.ENABLED_INTERNAL_SRS_CONTROLS)
            //     {
            //         if (_clientStateSingleton.PlayerUnitState.simultaneousTransmission
            //             && _clientStateSingleton.PlayerUnitState.simultaneousTransmissionControl ==
            //             PlayerUnitState.SimultaneousTransmissionControl.ENABLED_INTERNAL_SRS_CONTROLS)
            //         {
            //             var simulTransmission = 0;
            //             for (var i = 0; i < _clientStateSingleton.PlayerUnitState.Radios.Count; i++)
            //                 if (_clientStateSingleton.PlayerUnitState.Radios[i].SimultaneousTransmission)
            //                     if (i != RadioId)
            //                         simulTransmission++;
            //
            //             if (simulTransmission < MaxSimultaneousTransmissions)
            //             {
            //                 ToggleSimultaneousTransmissionButton.IsEnabled = true;
            //             }
            //             else
            //             {
            //                 ToggleSimultaneousTransmissionButton.IsEnabled = false;
            //                 ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
            //             }
            //         }
            //         else
            //         {
            //             ToggleSimultaneousTransmissionButton.IsEnabled = false;
            //             ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
            //         }
            //     }
            //     else
            //     {
            //         ToggleSimultaneousTransmissionButton.IsEnabled = false;
            //         ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
            //     }
            // }
            // else
            // {
            //     Up10.Visibility = Visibility.Hidden;
            //     Up1.Visibility = Visibility.Hidden;
            //     Up01.Visibility = Visibility.Hidden;
            //     Up001.Visibility = Visibility.Hidden;
            //     Up0001.Visibility = Visibility.Hidden;
            //
            //     Down10.Visibility = Visibility.Hidden;
            //     Down1.Visibility = Visibility.Hidden;
            //     Down01.Visibility = Visibility.Hidden;
            //     Down001.Visibility = Visibility.Hidden;
            //     Down0001.Visibility = Visibility.Hidden;
            //
            //     ToggleSimultaneousTransmissionButton.IsEnabled = false;
            //     ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
            //
            //     ChannelTab.Visibility = Visibility.Collapsed;
            // }
        }

        internal void RepaintRadioStatus()
        {
          //  HandleRetransmitStatus();

        //     var dcsPlayerRadioInfo = _clientStateSingleton.PlayerUnitState;
        //
        //     if (!_clientStateSingleton.IsConnected || dcsPlayerRadioInfo == null || !dcsPlayerRadioInfo.IsCurrent() ||
        //         RadioId > dcsPlayerRadioInfo.Radios.Count - 1)
        //     {
        //         RadioActive.Fill = new SolidColorBrush(Colors.Red);
        //         RadioLabel.Text = "No Radio";
        //         RadioFrequency.Text = "Not Connected";
        //
        //         RadioMetaData.Text = "";
        //
        //         RadioVolume.IsEnabled = false;
        //
        //         ToggleButtons(false);
        //
        //         ToggleSimultaneousTransmissionButton.IsEnabled = false;
        //         ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
        //
        //         //reset dragging just incase
        //         _dragging = false;
        //     }
        //     else
        //     {
        //         var currentRadio = dcsPlayerRadioInfo.Radios[RadioId];
        //         var transmitting = _clientStateSingleton.RadioSendingState;
        //
        //         if (transmitting.IsSending)
        //         {
        //             if (transmitting.SendingOn == RadioId)
        //                 RadioActive.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#96FF6D"));
        //             else if (currentRadio != null && currentRadio.SimultaneousTransmission)
        //                 RadioActive.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F86FF"));
        //         }
        //         else
        //         {
        //             if (RadioId == dcsPlayerRadioInfo.SelectedRadio)
        //                 RadioActive.Fill = new SolidColorBrush(Colors.Green);
        //             else if (currentRadio != null && currentRadio.SimultaneousTransmission)
        //                 RadioActive.Fill = new SolidColorBrush(Colors.DarkBlue);
        //             else
        //                 RadioActive.Fill = new SolidColorBrush(Colors.Orange);
        //         }
        //
        //         if (currentRadio == null || currentRadio.Modulation == RadioConfig.Modulation.DISABLED) // disabled
        //         {
        //             RadioActive.Fill = new SolidColorBrush(Colors.Red);
        //             RadioLabel.Text = "No Radio";
        //             RadioFrequency.Text = "Not Connected";
        //             RadioMetaData.Text = "";
        //
        //
        //             RadioVolume.IsEnabled = false;
        //
        //             ToggleButtons(false);
        //
        //             ToggleSimultaneousTransmissionButton.IsEnabled = false;
        //             ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
        //
        //             return;
        //         }
        //
        //         if (currentRadio.Modulation == RadioConfig.Modulation.INTERCOM) //intercom
        //         {
        //             RadioFrequency.Text = "INTERCOM";
        //             RadioMetaData.Text = "";
        //         }
        //         else if (currentRadio.Modulation == RadioConfig.Modulation.MIDS) //MIDS
        //         {
        //             RadioFrequency.Text = "MIDS";
        //             if (currentRadio.CurrentChannel >= 0)
        //                 RadioMetaData.Text = " CHN " + currentRadio.CurrentChannel;
        //             else
        //                 RadioMetaData.Text = " OFF";
        //         }
        //         else
        //         {
        //             if (!RadioFrequency.IsFocused
        //                 || currentRadio.Config.FrequencyControl == RadioConfig.FreqMode.COCKPIT
        //                 || currentRadio.Modulation == RadioConfig.Modulation.DISABLED)
        //                 RadioFrequency.Text =
        //                     (currentRadio.Frequency / MHz).ToString("0.000",
        //                         CultureInfo.InvariantCulture); //make number UK / US style with decimals not commas!
        //
        //             if (currentRadio.Modulation == RadioConfig.Modulation.AM)
        //                 RadioMetaData.Text = "AM";
        //             else if (currentRadio.Modulation == RadioConfig.Modulation.FM)
        //                 RadioMetaData.Text = "FM";
        //             else if (currentRadio.Modulation == RadioConfig.Modulation.HAVEQUICK)
        //                 RadioMetaData.Text = "HQ";
        //             else
        //                 RadioMetaData.Text += "";
        //
        //             if (currentRadio.SecondaryFrequency > 100) RadioMetaData.Text += " G";
        //
        //             if (currentRadio.CurrentChannel > -1) RadioMetaData.Text += " C" + currentRadio.CurrentChannel;
        //             if (currentRadio.Encrypted && currentRadio.EncryptionKey > 0)
        //                 RadioMetaData.Text += " E" + currentRadio.EncryptionKey; // ENCRYPTED
        //         }
        //
        //         RadioLabel.Text = dcsPlayerRadioInfo.Radios[RadioId].Name;
        //
        //         var count = _connectClientsSingleton.ClientsOnFreq(currentRadio.Frequency, currentRadio.Modulation);
        //
        //         if (count > 0) RadioMetaData.Text += " 👤" + count;
        //
        //         if (currentRadio.Config.VolumeControl == RadioConfig.VolumeMode.OVERLAY)
        //             RadioVolume.IsEnabled = true;
        //
        //         //reset dragging just incase
        //         //    _dragging = false;
        //         else
        //             RadioVolume.IsEnabled = false;
        //
        //         //reset dragging just incase
        //         //  _dragging = false;
        //
        //         ToggleButtons(currentRadio.Config.FrequencyControl == RadioConfig.FreqMode.OVERLAY);
        //
        //         if (_dragging == false) RadioVolume.Value = currentRadio.Volume * 100.0;
        //     }
        //
        //     var item = TabControl.SelectedItem as TabItem;
        //
        //     if (item?.Visibility != Visibility.Visible) TabControl.SelectedIndex = 0;
        // }
        //
        //
        // public void HandleRetransmitStatus()
        // {
        //     var serverSettings = SyncedServerSettings.Instance;
        //     var dcsPlayerRadioInfo = _clientStateSingleton.PlayerUnitState;
        //
        //     if (dcsPlayerRadioInfo != null && dcsPlayerRadioInfo.IsCurrent() && serverSettings.RetransmitNodeLimit > 0)
        //     {
        //         var currentRadio = dcsPlayerRadioInfo.Radios[RadioId];
        //
        //         if (currentRadio.Config.RetransmitControl == RadioConfig.RetransmitMode.DISABLED)
        //         {
        //             Retransmit.Visibility = Visibility.Hidden;
        //         }
        //         else if (currentRadio.Config.RetransmitControl == RadioConfig.RetransmitMode.COCKPIT)
        //         {
        //             Retransmit.Visibility = Visibility.Visible;
        //             Retransmit.IsEnabled = false;
        //         }
        //         else
        //         {
        //             Retransmit.Visibility = Visibility.Visible;
        //             Retransmit.IsEnabled = true;
        //         }
        //
        //         if (currentRadio.Retransmit)
        //             Retransmit.Foreground = new SolidColorBrush(Colors.Red);
        //         else
        //             Retransmit.Foreground = new SolidColorBrush(Colors.White);
        //     }
        //     else
        //     {
        //         Retransmit.Visibility = Visibility.Hidden;
        //     }
        }


        internal void RepaintRadioReceive()
        {
            TransmitterName.Visibility = Visibility.Collapsed;
            RadioFrequency.Visibility = Visibility.Visible;
            RadioMetaData.Visibility = Visibility.Visible;

            var dcsPlayerRadioInfo = _clientStateSingleton.PlayerUnitState;
            if (dcsPlayerRadioInfo == null)
            {
                RadioFrequency.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                RadioMetaData.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
            }
            else
            {
                var receiveState = _clientStateSingleton.RadioReceivingState[RadioId];
                //check if current

                if (receiveState == null || !receiveState.IsReceiving)
                {
                    RadioFrequency.Foreground =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                    RadioMetaData.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                }
                else if (receiveState != null && receiveState.IsReceiving)
                {
                    if (receiveState.SentBy.Length > 0)
                    {
                        TransmitterName.Text = receiveState.SentBy;

                        TransmitterName.Visibility = Visibility.Visible;
                        RadioFrequency.Visibility = Visibility.Collapsed;
                        RadioMetaData.Visibility = Visibility.Collapsed;
                    }

                    if (receiveState.IsSecondary)
                    {
                        TransmitterName.Foreground = new SolidColorBrush(Colors.Red);
                        RadioFrequency.Foreground = new SolidColorBrush(Colors.Red);
                        RadioMetaData.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        TransmitterName.Foreground = new SolidColorBrush(Colors.White);
                        RadioFrequency.Foreground = new SolidColorBrush(Colors.White);
                        RadioMetaData.Foreground = new SolidColorBrush(Colors.White);
                    }
                }
                else
                {
                    RadioFrequency.Foreground =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                    RadioMetaData.Foreground =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                }
            }
        }


        private void ToggleSimultaneousTransmissionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientStateSingleton.PlayerUnitState != null &&
                _clientStateSingleton.PlayerUnitState.simultaneousTransmission)
            {
                var currentRadio = RadioHelper.GetRadio(RadioId);

                if (currentRadio != null)
                {
                    currentRadio.SimultaneousTransmission = !currentRadio.SimultaneousTransmission;

                    if (currentRadio.SimultaneousTransmission)
                        ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.Orange);
                    else
                        ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
                }
            }
        }

        private void RetransmitClick(object sender, RoutedEventArgs e)
        {
            RadioHelper.ToggleRetransmit(RadioId);
        }
    }
}