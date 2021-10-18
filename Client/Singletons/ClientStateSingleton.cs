using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
using Ciribob.SRS.Client.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings.RadioChannels;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.RadioOverlayWindow.PresetChannels;
using Ciribob.SRS.Common;
using Ciribob.SRS.Common.DCSState;
using Ciribob.SRS.Common.Network;
using Ciribob.SRS.Common.Setting;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons
{
    public sealed class ClientStateSingleton : INotifyPropertyChanged
    {
        private static volatile ClientStateSingleton _instance;
        private static object _lock = new Object();

        public delegate bool RadioUpdatedCallback();

        private List<RadioUpdatedCallback> _radioCallbacks = new List<RadioUpdatedCallback>();

        public event PropertyChangedEventHandler PropertyChanged;

        public PlayerRadioInfo PlayerRadioInfo { get; }
        public PlayerSideInfo PlayerCoaltionLocationMetadata { get; set; }

        // Timestamp the last UDP Game GUI broadcast was received from DCS, used for determining active game connection
        public long DcsGameGuiLastReceived { get; set; }

        // Timestamp the last UDP Export broadcast was received from DCS, used for determining active game connection
        public long DcsExportLastReceived { get; set; }

        // Timestamp for the last time 
        public long LotATCLastReceived { get; set; }

        //store radio channels here?
        public PresetChannelsViewModel[] FixedChannels { get; }

        public long LastSent { get; set; }

        public long LastPostionCoalitionSent { get; set; }

        private static readonly DispatcherTimer _timer = new DispatcherTimer();

        public RadioSendingState RadioSendingState { get; set; }
        public  RadioReceivingState[] RadioReceivingState { get; }

        private bool isConnected;
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                isConnected = value;
                NotifyPropertyChanged("IsConnected");
            }
        }

        private bool isVoipConnected;
        public bool IsVoipConnected
        {
            get
            {
                return isVoipConnected;
            }
            set
            {
                isVoipConnected = value;
                NotifyPropertyChanged("IsVoipConnected");
            }
        }

        private bool isConnectionErrored;
        public string ShortGUID { get; }

        public bool IsConnectionErrored
        {
            get
            {
                return isConnectionErrored;
            }
            set
            {
                isConnectionErrored = value;
                NotifyPropertyChanged("isConnectionErrored");
            }
        }


        public string LastSeenName { get; set; }


        private ClientStateSingleton()
        {
            RadioSendingState = new RadioSendingState();
            RadioReceivingState = new RadioReceivingState[11];

            ShortGUID = ShortGuid.NewGuid();
            PlayerRadioInfo = new PlayerRadioInfo();
            PlayerCoaltionLocationMetadata = new PlayerSideInfo();

            // The following members are not updated due to events. Therefore we need to setup a polling action so that they are
            // periodically checked.
            DcsGameGuiLastReceived = 0;
            DcsExportLastReceived = 0;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => {
                NotifyPropertyChanged("IsGameConnected");
                NotifyPropertyChanged("IsLotATCConnected");
                NotifyPropertyChanged("ExternalAWACSModeConnected");
            };
            _timer.Start();

            FixedChannels = new PresetChannelsViewModel[10];

            for (int i = 0; i < FixedChannels.Length; i++)
            {
                FixedChannels[i] = new PresetChannelsViewModel(new FilePresetChannelsStore(), i + 1);
            }

            LastSent = 0;

            IsConnected = false;

            LastSeenName = Settings.GlobalSettingsStore.Instance.GetClientSetting(Settings.GlobalSettingsKeys.LastSeenName).RawValue;
        }

        public static ClientStateSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ClientStateSingleton();
                    }
                }

                return _instance;
            }
        }

        public int IntercomOffset { get; set; }

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

      

        public void UpdatePlayerPosition( LatLngPosition latLngPosition)
        {
            PlayerCoaltionLocationMetadata.LngLngPosition = latLngPosition;
            PlayerRadioInfo.latLng = latLngPosition;
            
        }

    }
}