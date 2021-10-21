using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Ciribob.SRS.Common;
using Ciribob.SRS.Common.Network;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Server.Network;
using NLog;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.UI.ClientAdmin
{
    public class ClientViewModel : Screen
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IEventAggregator _eventAggregator;

        public ClientViewModel(SRClient client, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            Client = client;
            Client.PropertyChanged += ClientOnPropertyChanged;
        }

        public SRClient Client { get; }

        public string ClientName => Client?.UnitState?.Name;

        public string TransmittingFrequency => Client.TransmittingFrequency;

        public bool ClientMuted => Client.Muted;

        public SolidColorBrush ClientCoalitionColour
        {
            get
            {
                switch (Client?.UnitState.Coalition)
                {
                    case 0:
                        return new SolidColorBrush(Colors.White);
                    case 1:
                        return new SolidColorBrush(Colors.Red);
                    case 2:
                        return new SolidColorBrush(Colors.Blue);
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }
        }

        private void ClientOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "Coalition")
            {
                NotifyOfPropertyChange(() => ClientCoalitionColour);
            }
            else if (propertyChangedEventArgs.PropertyName == "TransmittingFrequency")
            {
                NotifyOfPropertyChange(() => TransmittingFrequency);
            }
        }

        public void KickClient()
        {
            var messageBoxResult = MessageBox.Show($"Are you sure you want to Kick {Client.UnitState.Name}?", "Ban Confirmation",
                MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                _eventAggregator.PublishOnBackgroundThreadAsync(new KickClientMessage(Client));
            }
        }

        public void BanClient()
        {
            var messageBoxResult = MessageBox.Show($"Are you sure you want to Ban {Client.UnitState.Name}?", "Ban Confirmation",
                MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                _eventAggregator.PublishOnBackgroundThreadAsync(new BanClientMessage(Client));
            }
        }

        public void ToggleClientMute()
        {
            Client.Muted = !Client.Muted;
            NotifyOfPropertyChange(() => ClientMuted);
        }
    }
}