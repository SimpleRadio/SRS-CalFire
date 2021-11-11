using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Network.Models;
using Ciribob.SRS.Common.Network.Models;
using NLog;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.UI.ClientAdmin
{
    public class ClientViewModel : Screen
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IEventAggregator _eventAggregator;

        public ClientViewModel(SRClientBase client, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            Client = client;
            Client.PropertyChanged += ClientOnPropertyChanged;
        }

        public SRClientBase Client { get; }

        public string ClientName => Client?.UnitState?.Name;

        public string TransmittingFrequency => Client.TransmittingFrequency;

        public bool ClientMuted => Client.Muted;


        private void ClientOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "TransmittingFrequency")
                NotifyOfPropertyChange(() => TransmittingFrequency);
        }

        public void KickClient()
        {
            var messageBoxResult = MessageBox.Show($"Are you sure you want to Kick {Client.UnitState.Name}?",
                "Ban Confirmation",
                MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
                _eventAggregator.PublishOnBackgroundThreadAsync(new KickClientMessage(Client));
        }

        public void BanClient()
        {
            var messageBoxResult = MessageBox.Show($"Are you sure you want to Ban {Client.UnitState.Name}?",
                "Ban Confirmation",
                MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
                _eventAggregator.PublishOnBackgroundThreadAsync(new BanClientMessage(Client));
        }

        public void ToggleClientMute()
        {
            Client.Muted = !Client.Muted;
            NotifyOfPropertyChange(() => ClientMuted);
        }
    }
}