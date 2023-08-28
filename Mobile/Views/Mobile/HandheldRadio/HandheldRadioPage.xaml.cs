using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile;

public partial class HandheldRadioPage : ContentPage
{
    private readonly IDispatcherTimer _updateTimer;
    private readonly SRSAudioManager _srsAudioManager;

    public HandheldRadioPage(SRSAudioManager srsAudioManager)
    {
        _srsAudioManager = srsAudioManager;
        InitializeComponent();
        DeviceDisplay.Current.KeepScreenOn = true;

        BindingContext = new RadioViewModel(1);

        _updateTimer = Application.Current.Dispatcher.CreateTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
        _updateTimer.Tick += (s, e) => ((RadioViewModel)BindingContext).RefreshView();
    }

    private void Button_OnPressed(object sender, EventArgs e)
    {
        if (_srsAudioManager != null)
            _srsAudioManager.PTTPressed = true;
    }

    private void Button_OnReleased(object sender, EventArgs e)
    {
        if (_srsAudioManager != null)
            _srsAudioManager.PTTPressed = false;
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
}