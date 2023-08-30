﻿

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.Settings;

/// <summary>
///     Interaction logic for RadioChannelConfigUI.xaml
/// </summary>
public partial class RadioChannelConfigUi : ContentView
{

    public double Volume
    {
        get => (double)GetValue(VolumeProperty);
        set => SetValue(VolumeProperty,(Double) value);
    }
    
    
    public static readonly BindableProperty VolumeProperty =
        BindableProperty.Create(nameof(Volume), typeof(double), typeof(Slider));

    public RadioChannelConfigUi()
    {
        InitializeComponent();
    }
    //
    // public static float GetCornerRadius(Button element)
    // {
    //     return (CornerRadius)element.GetValue(CornerRadiusProperty);
    // }
    //
    // public static readonly BindableProperty CornerRadiusProperty =
    //     BindableProperty.CreateAttached(
    //         "VolumeValue",
    //         typeof(float),
    //         typeof(ButtonHelperClass),
    //         0
    //     );
    //
    // public float VolumeValue
    // {
    //     set => SetValue(VolumeSliderDependencyProperty, value);
    //     get
    //     {
    //         var val = (float)GetValue(VolumeSliderDependencyProperty);
    //         return val;
    //     }
    // }
}