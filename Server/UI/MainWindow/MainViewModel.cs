using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using Ciribob.SRS.Common;
using Ciribob.SRS.Common.Setting;
using Ciribob.SRS.Server.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Server.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Server.UI.ClientAdmin;
using NLog;
using LogManager = NLog.LogManager;

namespace Ciribob.DCS.SimpleRadio.Standalone.Server.UI.MainWindow
{
    public sealed class MainViewModel : Screen, IHandle<ServerStateMessage>
    {
        private readonly ClientAdminViewModel _clientAdminViewModel;
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowManager _windowManager;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private DispatcherTimer _passwordDebounceTimer = null;

        public MainViewModel(IWindowManager windowManager, IEventAggregator eventAggregator,
            ClientAdminViewModel clientAdminViewModel)
        {
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _clientAdminViewModel = clientAdminViewModel;
            _eventAggregator.Subscribe(this);

            DisplayName = $"DCS-SRS Server - {UpdaterChecker.VERSION} - {ListeningPort}" ;

            Logger.Info("DCS-SRS Server Running - " + UpdaterChecker.VERSION);
        }

        public bool IsServerRunning { get; private set; } = true;

        public string ServerButtonText => IsServerRunning ? "Stop Server" : "Start Server";

        public int NodeLimit
        {
            get => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.RETRANSMISSION_NODE_LIMIT).IntValue;
            set
            {
                ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.RETRANSMISSION_NODE_LIMIT,
                    value.ToString());
                _eventAggregator.PublishOnBackgroundThread(new ServerSettingsChangedMessage());
            }
        }

        public int ClientsCount { get; private set; }

        public string LOSText
            => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.LOS_ENABLED).BoolValue ? "ON" : "OFF";

        public string DistanceLimitText
            => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.DISTANCE_ENABLED).BoolValue ? "ON" : "OFF";

        public string RealRadioText
            => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.IRL_RADIO_TX).BoolValue ? "ON" : "OFF";

        public string IRLRadioRxText
            => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.IRL_RADIO_RX_INTERFERENCE).BoolValue ? "ON" : "OFF";

        private string _testFrequencies =
            ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.TEST_FREQUENCIES).StringValue;

        private DispatcherTimer _testFrequenciesDebounceTimer;

        public string TestFrequencies
        {
            get { return _testFrequencies; }
            set
            {
                _testFrequencies = value.Trim();
                if (_testFrequenciesDebounceTimer != null)
                {
                    _testFrequenciesDebounceTimer.Stop();
                    _testFrequenciesDebounceTimer.Tick -= TestFrequenciesDebounceTimerTick;
                    _testFrequenciesDebounceTimer = null;
                }

                _testFrequenciesDebounceTimer = new DispatcherTimer();
                _testFrequenciesDebounceTimer.Tick += TestFrequenciesDebounceTimerTick;
                _testFrequenciesDebounceTimer.Interval = TimeSpan.FromMilliseconds(500);
                _testFrequenciesDebounceTimer.Start();

                NotifyOfPropertyChange(() => TestFrequencies);
            }
        }

        private DispatcherTimer _globalLobbyFrequenciesDebounceTimer;


        public string TunedCountText
            => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.SHOW_TUNED_COUNT).BoolValue ? "ON" : "OFF";

        public string ShowTransmitterNameText
            => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.SHOW_TRANSMITTER_NAME).BoolValue ? "ON" : "OFF";
        public string ListeningPort
            => ServerSettingsStore.Instance.GetServerSetting(ServerSettingsKeys.SERVER_PORT).StringValue;

        public void Handle(ServerStateMessage message)
        {
            IsServerRunning = message.IsRunning;
            ClientsCount = message.Count;
        }

        public void ServerStartStop()
        {
            if (IsServerRunning)
            {
                _eventAggregator.PublishOnBackgroundThread(new StopServerMessage());
            }
            else
            {
                _eventAggregator.PublishOnBackgroundThread(new StartServerMessage());
            }
        }

        public void ShowClientList()
        {
            IDictionary<string, object> settings = new Dictionary<string, object>
            {
                {"Icon", new BitmapImage(new Uri("pack://application:,,,/SR-Server;component/server-10.ico"))},
                {"ResizeMode", ResizeMode.CanMinimize}
            };
            _windowManager.ShowWindow(_clientAdminViewModel, null, settings);
        }


        public void LOSToggle()
        {
            var newSetting = LOSText != "ON";
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.LOS_ENABLED, newSetting);
            NotifyOfPropertyChange(() => LOSText);

            _eventAggregator.PublishOnBackgroundThread(new ServerSettingsChangedMessage());
        }

        public void DistanceLimitToggle()
        {
            var newSetting = DistanceLimitText != "ON";
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.DISTANCE_ENABLED, newSetting);
            NotifyOfPropertyChange(() => DistanceLimitText);

            _eventAggregator.PublishOnBackgroundThread(new ServerSettingsChangedMessage());
        }

        public void RealRadioToggle()
        {
            var newSetting = RealRadioText != "ON";
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.IRL_RADIO_TX, newSetting);
            NotifyOfPropertyChange(() => RealRadioText);

            _eventAggregator.PublishOnBackgroundThread(new ServerSettingsChangedMessage());
        }

        public void IRLRadioRxBehaviourToggle()
        {
            var newSetting = IRLRadioRxText != "ON";
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.IRL_RADIO_RX_INTERFERENCE, newSetting);
            NotifyOfPropertyChange(() => IRLRadioRxText);

            _eventAggregator.PublishOnBackgroundThread(new ServerSettingsChangedMessage());
        }


        private void TestFrequenciesDebounceTimerTick(object sender, EventArgs e)
        {
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.TEST_FREQUENCIES, _testFrequencies);

            _eventAggregator.PublishOnBackgroundThread(new ServerFrequenciesChanged()
            {
                TestFrequencies = _testFrequencies
            });

            _testFrequenciesDebounceTimer.Stop();
            _testFrequenciesDebounceTimer.Tick -= TestFrequenciesDebounceTimerTick;
            _testFrequenciesDebounceTimer = null;
        }


        public void TunedCountToggle()
        {
            var newSetting = TunedCountText != "ON";
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.SHOW_TUNED_COUNT, newSetting);
            NotifyOfPropertyChange(() => TunedCountText);

            _eventAggregator.PublishOnBackgroundThread(new ServerSettingsChangedMessage());
        }

        public void ShowTransmitterNameToggle()
        {
            var newSetting = ShowTransmitterNameText != "ON";
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.SHOW_TRANSMITTER_NAME, newSetting);
            NotifyOfPropertyChange(() => ShowTransmitterNameText);

            _eventAggregator.PublishOnBackgroundThread(new ServerSettingsChangedMessage());
        }
    }
}