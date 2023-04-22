using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Network.Models;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.UI.ClientAdmin;
using NLog;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.UI.MainWindow
{
    public sealed class MainViewModel : Screen, IHandle<ServerStateMessage>
    {
        private readonly ClientAdminViewModel _clientAdminViewModel;
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowManager _windowManager;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private string _serverRecordingFrequencies =
            ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.SERVER_RECORDING_FREQUENCIES).StringValue;

        private DispatcherTimer _serverRecordingFrequenciesDebounceTimer;

        private string _testFrequencies =
            ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.TEST_FREQUENCIES).StringValue;

        private DispatcherTimer _testFrequenciesDebounceTimer;

        public MainViewModel(IWindowManager windowManager, IEventAggregator eventAggregator,
            ClientAdminViewModel clientAdminViewModel)
        {
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _clientAdminViewModel = clientAdminViewModel;
            _eventAggregator.Subscribe(this);

            DisplayName = $"SRS Server - {UpdaterChecker.VERSION} - {ListeningPort}";

            Logger.Info("SRS Server Running - " + UpdaterChecker.VERSION);
        }

        public bool IsServerRunning { get; private set; } = true;

        public string ServerButtonText => IsServerRunning ? "Stop Server" : "Start Server";

        public int ClientsCount { get; private set; }

        public string RealRadioText
            => ServerSettingsStore.Instance.GetGeneralSetting(ServerSettingsKeys.IRL_RADIO_TX).BoolValue ? "ON" : "OFF";

        public string TestFrequencies
        {
            get => _testFrequencies;
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

        public string ServerRecordingFrequencies
        {
            get => _serverRecordingFrequencies;
            set
            {
                _serverRecordingFrequencies = value.Trim();
                if (_serverRecordingFrequenciesDebounceTimer != null)
                {
                    _serverRecordingFrequenciesDebounceTimer.Stop();
                    _serverRecordingFrequenciesDebounceTimer.Tick -= ServerRecordingFrequenciesDebounceTimerTick;
                    _serverRecordingFrequenciesDebounceTimer = null;
                }

                _serverRecordingFrequenciesDebounceTimer = new DispatcherTimer();
                _serverRecordingFrequenciesDebounceTimer.Tick += ServerRecordingFrequenciesDebounceTimerTick;
                _serverRecordingFrequenciesDebounceTimer.Interval = TimeSpan.FromMilliseconds(500);
                _serverRecordingFrequenciesDebounceTimer.Start();

                NotifyOfPropertyChange(() => ServerRecordingFrequencies);
            }
        }

        public string ListeningPort
            => ServerSettingsStore.Instance.GetServerSetting(ServerSettingsKeys.SERVER_PORT).StringValue;

        public async Task HandleAsync(ServerStateMessage message, CancellationToken token)
        {
            IsServerRunning = message.IsRunning;
            ClientsCount = message.Count;
            NotifyOfPropertyChange("ServerButtonText");
            NotifyOfPropertyChange("ClientsCount");
        }

        public void ServerStartStop()
        {
            if (IsServerRunning)
                _eventAggregator.PublishOnBackgroundThreadAsync(new StopServerMessage());
            else
                _eventAggregator.PublishOnBackgroundThreadAsync(new StartServerMessage());
        }

        public void ShowClientList()
        {
            IDictionary<string, object> settings = new Dictionary<string, object>
            {
                { "Icon", new BitmapImage(new Uri("pack://application:,,,/SRS-Server;component/server-10.ico")) },
                { "ResizeMode", ResizeMode.CanMinimize }
            };
            _windowManager.ShowWindowAsync(_clientAdminViewModel, null, settings);
        }


        public void RealRadioToggle()
        {
            var newSetting = RealRadioText != "ON";
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.IRL_RADIO_TX, newSetting);
            NotifyOfPropertyChange(() => RealRadioText);

            _eventAggregator.PublishOnBackgroundThreadAsync(new ServerSettingsChangedMessage());
        }

        private void TestFrequenciesDebounceTimerTick(object sender, EventArgs e)
        {
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.TEST_FREQUENCIES, _testFrequencies);

            _eventAggregator.PublishOnBackgroundThreadAsync(new ServerFrequenciesChanged
            {
                TestFrequencies = _testFrequencies
            });

            _testFrequenciesDebounceTimer.Stop();
            _testFrequenciesDebounceTimer.Tick -= TestFrequenciesDebounceTimerTick;
            _testFrequenciesDebounceTimer = null;
        }

        private void ServerRecordingFrequenciesDebounceTimerTick(object sender, EventArgs e)
        {
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.SERVER_RECORDING_FREQUENCIES, _serverRecordingFrequencies);

            _eventAggregator.PublishOnBackgroundThreadAsync(new ServerFrequenciesChanged
            {
                TestFrequencies = _testFrequencies
            });

            _serverRecordingFrequenciesDebounceTimer.Stop();
            _serverRecordingFrequenciesDebounceTimer.Tick -= ServerRecordingFrequenciesDebounceTimerTick;
            _serverRecordingFrequenciesDebounceTimer = null;
        }
    }
}