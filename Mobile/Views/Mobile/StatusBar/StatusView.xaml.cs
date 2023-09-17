using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.Settings;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.StatusBar;

public partial class StatusView : ContentView
{
    public StatusView()
    {
        InitializeComponent();
    }
    

    private void Button_OnClicked(object sender, EventArgs e)
    {
        Toast.Make("Loading Settings", ToastDuration.Short, 14).Show();
        Navigation.PushAsync(new ClientSettingsPage(), true);
    }
}