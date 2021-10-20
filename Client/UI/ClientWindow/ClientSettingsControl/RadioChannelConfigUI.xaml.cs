﻿using System;
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
 

            //I do this because at this point ProfileSettingKey hasn't been set
            //but it has when this is called
            // ChannelSelector.Loaded += InitBalanceSlider;
        }

        public static readonly DependencyProperty VolumeSliderDependencyProperty =
            DependencyProperty.Register("VolumeValue", typeof(float), typeof(RadioChannelConfigUi),
                new FrameworkPropertyMetadata((float)0)
            );

        //

        public float VolumeValue
        {
            set
            {
                SetValue(VolumeSliderDependencyProperty, value);
            }
            get
            {
                float val = (float)GetValue(VolumeSliderDependencyProperty);
                return val;
            }
        }

        // private void InitBalanceSlider(object sender, RoutedEventArgs e)
        // {
        //     ChannelSelector.IsEnabled = false;
        //     // Reload();
        //     //
        //     // ChannelSelector.ValueChanged += ChannelSelector_SelectionChanged;
        // }
        //
        // public void Reload()
        // {
        //     ChannelSelector.IsEnabled = false;
        //
        //     ChannelSelector.Value = GlobalSettingsStore.Instance.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingKey);
        //
        //     ChannelSelector.IsEnabled = true;
        // }
        //
        // private void ChannelSelector_SelectionChanged(object sender, EventArgs eventArgs)
        // {
        //     this.DataContext.GetHashCode();
        //     //the selected value changes when 
        //     if (ChannelSelector.IsEnabled)
        //     {
        //         GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingKey,(float) ChannelSelector.Value);
        //     }
        // }
    }
}