using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ciribob.SRS.Client.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.SRS.Common;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.AwacsRadioOverlayWindow
{
    /// <summary>
    ///     Interaction logic for IntercomControlGroup.xaml
    /// </summary>
    public partial class IntercomControlGroup : UserControl
    {
        private bool _dragging;

        private bool _init = true;
        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

        public IntercomControlGroup()
        {
            InitializeComponent();
        }

        public int RadioId { private get; set; }

        private void RadioSelectSwitch(object sender, RoutedEventArgs e)
        {
            var currentRadio = _clientStateSingleton.PlayerRadioInfo.radios[RadioId];

            if (currentRadio.modulation != RadioInformation.Modulation.DISABLED)
            {
                if (_clientStateSingleton.PlayerRadioInfo.control ==
                    PlayerRadioInfo.RadioSwitchControls.HOTAS)
                {
                    _clientStateSingleton.PlayerRadioInfo.selected = (short) RadioId;
                }
            }
        }

        private void RadioVolume_DragStarted(object sender, RoutedEventArgs e)
        {
            _dragging = true;
        }


        private void RadioVolume_DragCompleted(object sender, RoutedEventArgs e)
        {
            var currentRadio = _clientStateSingleton.PlayerRadioInfo.radios[RadioId];

            if (currentRadio.modulation != RadioInformation.Modulation.DISABLED)
            {
                if (currentRadio.volMode == RadioInformation.VolumeMode.OVERLAY)
                {
                    var clientRadio = _clientStateSingleton.PlayerRadioInfo.radios[RadioId];

                    clientRadio.volume = (float) RadioVolume.Value / 100.0f;
                }
            }

            _dragging = false;
        }

        internal void RepaintRadioStatus()
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.PlayerRadioInfo;

            if (!_clientStateSingleton.IsConnected || (dcsPlayerRadioInfo == null) || !dcsPlayerRadioInfo.IsCurrent())
            {
                RadioActive.Fill = new SolidColorBrush(Colors.Red);

                RadioVolume.IsEnabled = false;

                //reset dragging just incase
                _dragging = false;

                IntercomNumberSpinner.IsEnabled = false;
            }
            else
            {
                var transmitting = _clientStateSingleton.RadioSendingState;
                var receiveState = _clientStateSingleton.RadioReceivingState[RadioId];

                if ((receiveState != null) && receiveState.IsReceiving)
                {
                    RadioActive.Fill = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#96FF6D"));
                }
                else if (RadioId == dcsPlayerRadioInfo.selected)
                {
                    if (transmitting.IsSending && (transmitting.SendingOn == RadioId))
                    {
                        RadioActive.Fill = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#96FF6D"));
                    }
                    else
                    {
                        RadioActive.Fill = new SolidColorBrush(Colors.Green);
                    }
                }

                else
                {
                    RadioActive.Fill = new SolidColorBrush(Colors.Orange);
                }

                var currentRadio = dcsPlayerRadioInfo.radios[RadioId];

                if (currentRadio.modulation == RadioInformation.Modulation.INTERCOM) //intercom
                {
                    RadioLabel.Text = "INTERCOM";

                    RadioVolume.IsEnabled = currentRadio.volMode == RadioInformation.VolumeMode.OVERLAY;

                    if (dcsPlayerRadioInfo.unitId >= PlayerRadioInfo.UnitIdOffset)
                    {
                        IntercomNumberSpinner.IsEnabled = true;
                        IntercomNumberSpinner.Value = _clientStateSingleton.IntercomOffset;
                    }
                    else
                    {
                        IntercomNumberSpinner.IsEnabled = false;
                        IntercomNumberSpinner.Value = 1;
                        _clientStateSingleton.IntercomOffset = 1;
                    }
                }
                else
                {
                    RadioLabel.Text = "NO INTERCOM";
                    RadioActive.Fill = new SolidColorBrush(Colors.Red);
                    RadioVolume.IsEnabled = false;
                    IntercomNumberSpinner.Value = 1;
                    IntercomNumberSpinner.IsEnabled = false;
                    _clientStateSingleton.IntercomOffset = 1;
                }

                if (_dragging == false)
                {
                    RadioVolume.Value = currentRadio.volume * 100.0;
                }
            }
        }

        private void IntercomNumber_SpinnerChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_init)
            {
                //ignore
                _init = false;
                return;
            }
            var dcsPlayerRadioInfo = _clientStateSingleton.PlayerRadioInfo;

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent() &&
                (dcsPlayerRadioInfo.unitId >= PlayerRadioInfo.UnitIdOffset))
            {
                _clientStateSingleton.IntercomOffset = (int) IntercomNumberSpinner.Value;
                dcsPlayerRadioInfo.unitId =
                    (uint) (PlayerRadioInfo.UnitIdOffset + _clientStateSingleton.IntercomOffset);
                _clientStateSingleton.LastSent = 0; //force refresh
            }
        }
    }
}