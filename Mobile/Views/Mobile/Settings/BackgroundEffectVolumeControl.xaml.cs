namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.Settings;

/// <summary>
///     Interaction logic for BackgroundEffectVolumeControl.xaml
/// </summary>
public partial class BackgroundEffectVolumeControl : ContentView
{
    public double Volume
    {
        get => (double)GetValue(VolumeProperty);
        set => SetValue(VolumeProperty,(Double) value);
    }
    
    public static readonly BindableProperty VolumeProperty =
        BindableProperty.Create(nameof(Volume), typeof(double), typeof(Slider));

    public BackgroundEffectVolumeControl()
    {
        InitializeComponent();
    }
}