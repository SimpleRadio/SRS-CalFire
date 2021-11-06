using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons.Models;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Singletons;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.AircraftOverlayWindow
{
    public class MultiRadioOverlayWindowViewModel: PropertyChangedBase
    {
        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;
        private readonly ConnectedClientsSingleton _connectClientsSingleton = ConnectedClientsSingleton.Instance;
        private DispatcherTimer _updateTimer;

        public ICommand RadioSelect { get; set; }

        public MultiRadioOverlayWindowViewModel()
        {
            //TODO
            // Add support for intercom
            // Add support for opacity
            // Add support for hotkeys

            RadioSelect = new DelegateCommand(() =>
            {
                RadioHelper.SelectRadio(0);
                NotifyPropertyChanged(nameof(Radio));
            }, () => IsAvailable);
        }

        public Radio Radio => ClientStateSingleton.Instance.PlayerUnitState.Radios[0];

        public void Start()
        {
            Stop();
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _updateTimer.Tick += RefreshView;
            _updateTimer.Start();
        }


        public void Stop()
        {
            _updateTimer?.Stop();
            _updateTimer = null;
        }

        private void RefreshView(object? sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(RadioActiveFill));
        }

        public bool IsAvailable
        {
            get
            {
                var radio = Radio;

                if (radio == null)
                {
                    return false;
                }

                return radio.Modulation != Modulation.DISABLED;
            }
        }

        public SolidColorBrush RadioActiveFill
        {
            get
            {
                if (Radio == null || !IsAvailable)
                {
                    return new SolidColorBrush(Colors.Red);
                }

                if (ClientStateSingleton.Instance.PlayerUnitState.SelectedRadio != 0)
                {
                    return new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    if (ClientStateSingleton.Instance.RadioSendingState.IsSending &&
                        ClientStateSingleton.Instance.RadioSendingState.SendingOn == 0)
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#96FF6D"));
                    }
                    else
                    {
                        return new SolidColorBrush(Colors.Green);
                    }
                }
            }
        }

        public bool VolumeEnabled
        {
            get
            {
                if (IsAvailable)
                {
                    var currentRadio = Radio;

                    if (currentRadio != null && currentRadio.Config.VolumeControl == RadioConfig.VolumeMode.OVERLAY)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public float Volume
        {
            get
            {
                var currentRadio = Radio;

                if (currentRadio != null)
                {

                    return currentRadio.Volume * 100.0f;
                }

                return 0f;
            }
            set
            {
                var currentRadio = Radio;

                if (currentRadio != null)
                {
                    var clientRadio = _clientStateSingleton.PlayerUnitState.Radios[0];

                    clientRadio.Volume = value / 100.0f;
                }
            }
        }

        ~MultiRadioOverlayWindowViewModel()
        {
            _updateTimer?.Stop();
        }
    }
}
