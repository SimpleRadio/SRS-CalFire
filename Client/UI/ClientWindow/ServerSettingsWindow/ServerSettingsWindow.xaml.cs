using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Ciribob.SRS.Client.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings;
using Ciribob.SRS.Common.Setting;
using MahApps.Metro.Controls;
using NLog;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI
{
    /// <summary>
    ///     Interaction logic for ServerSettingsWindow.xaml
    /// </summary>
    public partial class ServerSettingsWindow : MetroWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly DispatcherTimer _updateTimer;

        private readonly SyncedServerSettings _serverSettings = SyncedServerSettings.Instance;

        public ServerSettingsWindow()
        {
            InitializeComponent();

            _updateTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            _updateTimer.Tick += UpdateUI;
            _updateTimer.Start();

            UpdateUI(null, null);
        }

        private void UpdateUI(object sender, EventArgs e)
        {
            var settings = _serverSettings;

            try
            {
                LineOfSight.Content = settings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED) ? "ON" : "OFF";

                Distance.Content = settings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED) ? "ON" : "OFF";

                RealRadio.Content = settings.GetSettingAsBool(ServerSettingsKeys.IRL_RADIO_TX) ? "ON" : "OFF";

                RadioRXInterference.Content =
                    settings.GetSettingAsBool(ServerSettingsKeys.IRL_RADIO_RX_INTERFERENCE) ? "ON" : "OFF";


                TunedClientCount.Content = settings.GetSettingAsBool(ServerSettingsKeys.SHOW_TUNED_COUNT) ? "ON" : "OFF";

                ShowTransmitterName.Content = settings.GetSettingAsBool(ServerSettingsKeys.SHOW_TRANSMITTER_NAME) ? "ON" : "OFF";

                ServerVersion.Content = SRSClientSyncHandler.ServerVersion;

                NodeLimit.Content = settings.RetransmitNodeLimit;
            }
            catch (IndexOutOfRangeException ex)
            {
                Logger.Warn("Missing Server Option - Connected to old server");
            }
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _updateTimer.Stop();
        }
    }
}