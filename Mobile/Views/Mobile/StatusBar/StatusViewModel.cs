using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Singleton;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.Settings;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using PropertyChangedBase = Ciribob.SRS.Common.Helpers.PropertyChangedBase;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.StatusBar;

public class StatusViewModel: PropertyChangedBase, IHandle<VOIPStatusMessage>, IHandle<TCPClientStatusMessage>
{
    public Color TCPTextColour
    {
        get
        {
            if (SRSConnectionManager.Instance.TCPConnected)
            {
                return Colors.Green;
            }

            return Colors.Red;
        }
    }
    public string TCPText => $"TCP: {(SRSConnectionManager.Instance.TCPConnected?"CONNECTED":"DISCONNECTED")}";

    public Color VOIPTextColour
    {
        get
        {
            if (SRSConnectionManager.Instance.UDPConnected)
            {
                return Colors.Green;
            }

            return Colors.Red;
        }
    }
    public string VOIPText => $"VOIP: {(SRSConnectionManager.Instance.UDPConnected?"CONNECTED":"DISCONNECTED")}";

    public Task HandleAsync(VOIPStatusMessage message, CancellationToken cancellationToken)
    {
        NotifyPropertyChanged(nameof(VOIPTextColour));
        NotifyPropertyChanged(nameof(VOIPText));
        
        return Task.CompletedTask;
    }

    public Task HandleAsync(TCPClientStatusMessage message, CancellationToken cancellationToken)
    {
        NotifyPropertyChanged(nameof(TCPTextColour));
        NotifyPropertyChanged(nameof(TCPText));
        
        return Task.CompletedTask;
    }

    public StatusViewModel()
    {
        EventBus.Instance.SubscribeOnUIThread(this);
    }

    public Command SettingsButton = new Command(() =>
    {
        
    });

    ~StatusViewModel()
    {
        EventBus.Instance.Unsubcribe(this);
    }
}