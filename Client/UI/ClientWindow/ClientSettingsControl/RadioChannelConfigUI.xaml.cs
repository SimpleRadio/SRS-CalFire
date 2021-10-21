using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI
{
    /// <summary>
    ///     Interaction logic for RadioChannelConfigUI.xaml
    /// </summary>
    public partial class RadioChannelConfigUi : UserControl
    {
        public RadioChannelConfigUi()
        {
            InitializeComponent();
        }

        //"VolumeValue" string must match the method name
        public static readonly DependencyProperty VolumeSliderDependencyProperty =
            DependencyProperty.Register("VolumeValue", typeof(float), typeof(RadioChannelConfigUi),
                new FrameworkPropertyMetadata((float)0)
            );

        public float VolumeValue
        {
            set => SetValue(VolumeSliderDependencyProperty, value);
            get
            {
                var val = (float)GetValue(VolumeSliderDependencyProperty);
                return val;
            }
        }
    }
}