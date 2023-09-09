using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Utility;
using Ciribob.SRS.Common.Network.Singletons;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.AircraftRadio;

public partial class AircraftRadioPage : ContentPage
{
    private readonly IDispatcherTimer _updateTimer;

    public AircraftRadioPage()
    {
        InitializeComponent();

        BindingContext = new AircraftRadioPageViewModel();

        DeviceDisplay.Current.KeepScreenOn = true;

        Radio1.BindingContext = new RadioViewModel(1);
        Radio2.BindingContext = new RadioViewModel(2);
        Radio3.BindingContext = new RadioViewModel(3);
        Radio4.BindingContext = new RadioViewModel(4);
        Radio5.BindingContext = new RadioViewModel(5);
        Radio6.BindingContext = new RadioViewModel(6);
        Radio7.BindingContext = new RadioViewModel(7);
        Radio8.BindingContext = new RadioViewModel(8);
        Radio9.BindingContext = new RadioViewModel(9);
        Radio10.BindingContext = new RadioViewModel(10);

        _updateTimer = Application.Current.Dispatcher.CreateTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(100);

        _updateTimer.Tick += OnUpdateTimerOnTick;
    }

    private void OnUpdateTimerOnTick(object s, EventArgs e)
    {
        try
        {
            ((RadioViewModel)Radio1.BindingContext).RefreshView();
            ((RadioViewModel)Radio2.BindingContext).RefreshView();
            ((RadioViewModel)Radio3.BindingContext).RefreshView();
            ((RadioViewModel)Radio4.BindingContext).RefreshView();
            ((RadioViewModel)Radio5.BindingContext).RefreshView();
            ((RadioViewModel)Radio6.BindingContext).RefreshView();
            ((RadioViewModel)Radio7.BindingContext).RefreshView();
            ((RadioViewModel)Radio8.BindingContext).RefreshView();
            ((RadioViewModel)Radio9.BindingContext).RefreshView();
            ((RadioViewModel)Radio10.BindingContext).RefreshView();

            ((AircraftRadioPageViewModel)BindingContext).RefreshView();
        }
        catch (NullReferenceException ex)
        {
            //ignore
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _updateTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _updateTimer.Stop();
    }

    private void Transmit_OnReleased(object sender, EventArgs e)
    {
        EventBus.Instance.PublishOnBackgroundThreadAsync(new PTTState { PTTPressed = false });
    }

    private void Transmit_OnPressed(object sender, EventArgs e)
    {
        EventBus.Instance.PublishOnBackgroundThreadAsync(new PTTState { PTTPressed = true });
    }
}