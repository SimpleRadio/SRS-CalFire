﻿using System.Net;
using System.Net.Sockets;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Singleton;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.AircraftRadio;
using Ciribob.SRS.Common.Network.Client;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using CommunityToolkit.Maui.Alerts;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile;

public partial class HomePage : ContentPage, IHandle<TCPClientStatusMessage>
{
    private bool _isTransitioning;
    private bool connected;

    public HomePage()
    {
        InitializeComponent();

        var status = CheckAndRequestMicrophonePermission();

        /*
         * SET STATICS on singletons BEFORE load to set up the platform specific bits
         */

        //https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-system-helpers?tabs=windows
        //Set path to use writeable app data directory
        GlobalSettingsStore.Path = FileSystem.Current.AppDataDirectory + Path.DirectorySeparatorChar;

        //Load from APK itself (magic loader using streaming memory)
        CachedAudioEffectProvider.CachedEffectsLoader = delegate(string name)
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(name);
            var memStream = new MemoryStream();
            stream.Result.CopyTo(memStream);
            memStream.Position = 0;
            stream.Dispose();

            return memStream;
        };

        /*
         *
         */
        EventBus.Instance.SubscribeOnUIThread(this);
    }

    public Task HandleAsync(TCPClientStatusMessage message, CancellationToken cancellationToken)
    {
        MainThread.BeginInvokeOnMainThread(() => { 
            if (message.Connected)
            {
                connected = true;
                ConnectDisconnect.Text = "Disconnect";
            }
            else
            {
                connected = false;
                ConnectDisconnect.Text = "Connect";
            }

            ConnectDisconnect.IsEnabled = true;
            
        });
        

        return Task.CompletedTask;
    }

    public async Task<PermissionStatus> CheckAndRequestMicrophonePermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

        if (status == PermissionStatus.Granted)
            return status;

        if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            // Prompt the user to turn on in settings
            // On iOS once a permission has been denied it may not be requested again from the application
            return status;

        if (Permissions.ShouldShowRationale<Permissions.Microphone>())
        {
            // Prompt the user with additional information as to why the permission is needed
        }

        status = await Permissions.RequestAsync<Permissions.Microphone>();

        return status;
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        if (connected)
        {
            EventBus.Instance.PublishOnBackgroundThreadAsync(new DisconnectRequestMessage());
            ConnectDisconnect.IsEnabled = false;
            ConnectDisconnect.Text = "Disconnecting...";
        }
        else
        {

            var ipEndPoint = GetConnectionIP(Address.Text);
            
            if (ipEndPoint != null)
            {
                ConnectDisconnect.IsEnabled = false;
                ConnectDisconnect.Text = "Connecting...";

                SRSConnectionManager.Instance.StartAndConnect(ipEndPoint);
            }
            else
            {
                DisplayAlert("Error", "Host or Invalid IP and port", "OK");
            }
        }
    }
    
    private int GetPortFromString(string input)
    {
        var addr = input.Trim();

        if (addr.Contains(":"))
        {
            int port;
            if (int.TryParse(addr.Split(':')[1], out port)) return port;

            throw new ArgumentException("specified port is  invalid");
        }

        return 5002;
    }

    private IPEndPoint GetConnectionIP(string input)
    {
        var addr = input.Trim().ToLowerInvariant();
        //strip port
        if (addr.Contains(':'))
        {
            addr = addr.Split(':')[0];
        }
        //process hostname
        var resolvedAddresses = Dns.GetHostAddresses(addr);
        var ip = resolvedAddresses.FirstOrDefault(xa =>
            xa.AddressFamily ==
            AddressFamily
                .InterNetwork); // Ensure we get an IPv4 address in case the host resolves to both IPv6 and IPv4

        if (ip != null)
        {
            try
            {
                int port = GetPortFromString(input);

                return new(ip, port);
            }
            catch (ArgumentException ex)
            {
                return null;
            }
        }
        return null;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _isTransitioning = false;
    }

    private void Navigate_Clicked(object sender, EventArgs e)
    {
        if (!_isTransitioning)
        {
            _isTransitioning = true;
            ClientStateSingleton.Instance.PlayerUnitState.LoadHandHeldRadio();
            Toast.Make("Loading Handheld Radio").Show();
            Navigation.PushAsync(new HandheldRadioPage(), true);
        }
    }

    private void AircraftRadio_OnClicked(object sender, EventArgs e)
    {
        if (!_isTransitioning)
        {
            _isTransitioning = true;
            ClientStateSingleton.Instance.PlayerUnitState.LoadMultiRadio();
            Toast.Make("Loading Aircraft Radio").Show();
            Navigation.PushAsync(new AircraftRadioPage(), true);
        }
    }
}