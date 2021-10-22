using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.Common.PresetChannels;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
using Ciribob.SRS.Common.Network.Singletons;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.HandheldRadioOverlayWindow
{
    /// <summary>
    ///     Interaction logic for RadioControlGroup.xaml
    /// </summary>
    public partial class RadioControlGroup : UserControl
    {
        private const double MHz = 1000000;
        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;
        private readonly ConnectedClientsSingleton _connectClientsSingleton = ConnectedClientsSingleton.Instance;
        private bool _dragging;

        private int _radioId;

        public RadioControlGroup()
        {
            DataContext = this; // set data context

            InitializeComponent();
        }

        public PresetChannelsViewModel ChannelViewModel { get; set; }

        public int RadioId
        {
            get => _radioId;
            set
            {
                _radioId = value;
                UpdateBinding();
            }
        }

        //updates the binding so the changes are picked up for the linked FixedChannelsModel
        private void UpdateBinding()
        {
            //TODO fix
            // ChannelViewModel = _clientStateSingleton.FixedChannels[_radioId - 1];
            // if (ChannelViewModel != null)
            // {
            //     ChannelViewModel.Max =
            //         _clientStateSingleton.PlayerUnitState.Radios[RadioId].Config.MaxFrequency;
            //     ChannelViewModel.Min =
            //         _clientStateSingleton.PlayerUnitState.Radios[RadioId].Config.MinimumFrequency;
            // }


            var bindingExpression = PresetChannelsView.GetBindingExpression(DataContextProperty);
            bindingExpression?.UpdateTarget();
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
            // var currentRadio = _clientStateSingleton.PlayerUnitState.Radios[RadioId];
            //
            // if (currentRadio.Config.VolumeControl == RadioConfig.VolumeMode.OVERLAY)
            // {
            //     var clientRadio = _clientStateSingleton.PlayerUnitState.Radios[RadioId];
            //
            //     clientRadio.Volume = (float)RadioVolume.Value / 100.0f;
            // }

            _dragging = false;
        }

        private void ToggleButtons(bool enable)
        {
            if (enable)
            {
                Up10.Visibility = Visibility.Visible;
                Up1.Visibility = Visibility.Visible;
                Up01.Visibility = Visibility.Visible;
                Up001.Visibility = Visibility.Visible;
                Up0001.Visibility = Visibility.Visible;

                Down10.Visibility = Visibility.Visible;
                Down1.Visibility = Visibility.Visible;
                Down01.Visibility = Visibility.Visible;
                Down001.Visibility = Visibility.Visible;
                Down0001.Visibility = Visibility.Visible;

                Up10.IsEnabled = true;
                Up1.IsEnabled = true;
                Up01.IsEnabled = true;
                Up001.IsEnabled = true;
                Up0001.IsEnabled = true;

                Down10.IsEnabled = true;
                Down1.IsEnabled = true;
                Down01.IsEnabled = true;
                Down001.IsEnabled = true;
                Down0001.IsEnabled = true;

                //  ReloadButton.IsEnabled = true;
                //LoadFromFileButton.IsEnabled = true;

                PresetChannelsView.IsEnabled = true;

                ChannelTab.Visibility = Visibility.Visible;
            }
            else
            {
                Up10.Visibility = Visibility.Hidden;
                Up1.Visibility = Visibility.Hidden;
                Up01.Visibility = Visibility.Hidden;
                Up001.Visibility = Visibility.Hidden;
                Up0001.Visibility = Visibility.Hidden;

                Down10.Visibility = Visibility.Hidden;
                Down1.Visibility = Visibility.Hidden;
                Down01.Visibility = Visibility.Hidden;
                Down001.Visibility = Visibility.Hidden;
                Down0001.Visibility = Visibility.Hidden;

                PresetChannelsView.IsEnabled = false;

                ChannelTab.Visibility = Visibility.Collapsed;
            }
        }

        internal void RepaintRadioStatus()
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.PlayerUnitState;

            // if (dcsPlayerRadioInfo == null || !_clientStateSingleton.IsConnected)
            // {
            //     RadioActive.Fill = new SolidColorBrush(Colors.Red);
            //     RadioLabel.Text = "No Radio";
            //     RadioFrequency.Text = "Not Connected";
            //
            //     RadioVolume.IsEnabled = false;
            //
            //     TunedClients.Visibility = Visibility.Hidden;
            //
            //     ToggleButtons(false);
            //
            //     //reset dragging just incase
            //     _dragging = false;
            // }
            // else
            // {
            //     var currentRadio = dcsPlayerRadioInfo.Radios[RadioId];
            //
            //     if (currentRadio == null) return;
            //
            //     var transmitting = _clientStateSingleton.RadioSendingState;
            //     if (RadioId == dcsPlayerRadioInfo.SelectedRadio)
            //     {
            //         if (transmitting.IsSending && transmitting.SendingOn == RadioId)
            //             RadioActive.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#96FF6D"));
            //         else
            //             RadioActive.Fill = new SolidColorBrush(Colors.Green);
            //     }
            //     else
            //     {
            //         RadioActive.Fill = new SolidColorBrush(Colors.Orange);
            //     }
            //
            //     if (currentRadio.Modulation == Modulation.DISABLED) // disabled
            //     {
            //         RadioActive.Fill = new SolidColorBrush(Colors.Red);
            //         RadioLabel.Text = "No Radio";
            //         RadioFrequency.Text = "Disabled";
            //
            //         RadioVolume.IsEnabled = false;
            //
            //         TunedClients.Visibility = Visibility.Hidden;
            //
            //         ToggleButtons(false);
            //
            //         ChannelTab.Visibility = Visibility.Collapsed;
            //         return;
            //     }
            //
            //
            //     if (currentRadio.Modulation == Modulation.AM ||
            //         currentRadio.Modulation == Modulation.FM)
            //     {
            //         RadioFrequency.Text =
            //             (currentRadio.Frequency / MHz).ToString("0.000",
            //                 CultureInfo.InvariantCulture); //make nuber UK / US style with decimals not commas!
            //
            //         if (currentRadio.Modulation == Modulation.AM)
            //             RadioFrequency.Text += "AM";
            //         else if (currentRadio.Modulation == Modulation.FM)
            //             RadioFrequency.Text += "FM";
            //         else if (currentRadio.Modulation == Modulation.HAVEQUICK)
            //             RadioFrequency.Text += "HQ";
            //         else
            //             RadioFrequency.Text += "";
            //
            //         if (currentRadio.SecondaryFrequency > 100) RadioFrequency.Text += " G";
            //
            //         // if (currentRadio.CurrentChannel >= 0) RadioFrequency.Text += " C" + currentRadio.CurrentChannel;
            //         //
            //         // if (currentRadio.Encrypted && currentRadio.EncryptionKey > 0)
            //         //     RadioFrequency.Text += " E" + currentRadio.EncryptionKey; // ENCRYPTED
            //     }
            //
            //     var count = _connectClientsSingleton.ClientsOnFreq(currentRadio.Frequency, currentRadio.Modulation);
            //
            //     if (count > 0)
            //     {
            //         TunedClients.Text = "👤" + count;
            //         TunedClients.Visibility = Visibility.Visible;
            //     }
            //     else
            //     {
            //         TunedClients.Visibility = Visibility.Hidden;
            //     }
            //
            //     // RadioLabel.Text = dcsPlayerRadioInfo.Radios[RadioId].Name;
            //
            //     // if (currentRadio.Config.VolumeControl == RadioConfig.VolumeMode.OVERLAY)
            //     //     RadioVolume.IsEnabled = true;
            //
            //     //reset dragging just incase
            //     //    _dragging = false;
            //     // else
            //     //     RadioVolume.IsEnabled = false;
            //
            //     //reset dragging just incase
            //     //  _dragging = false;
            //
            //     //ToggleButtons(currentRadio.Config.FrequencyControl == RadioConfig.FreqMode.OVERLAY);
            //
            //  //   if (_dragging == false) RadioVolume.Value = currentRadio.Volume * 100.0;
            // }
            //
            // var item = TabControl.SelectedItem as TabItem;
            //
            // if (item?.Visibility != Visibility.Visible) TabControl.SelectedIndex = 0;
        }


        internal void RepaintRadioReceive()
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.PlayerUnitState;
            if (dcsPlayerRadioInfo == null)
            {
                RadioFrequency.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
            }
            else
            {
                var receiveState = _clientStateSingleton.RadioReceivingState[RadioId];
                //check if current

                if (receiveState == null || !receiveState.IsReceiving)
                {
                    RadioFrequency.Foreground =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                }
                else if (receiveState != null && receiveState.IsReceiving)
                {
                    if (receiveState.SentBy.Length > 0) RadioFrequency.Text = receiveState.SentBy;

                    if (receiveState.IsSecondary)
                        RadioFrequency.Foreground = new SolidColorBrush(Colors.Red);
                    else
                        RadioFrequency.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    RadioFrequency.Foreground =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                }
            }
        }


        private void RetransmitClick(object sender, RoutedEventArgs e)
        {
            RadioHelper.ToggleRetransmit(RadioId);
        }
    }
}