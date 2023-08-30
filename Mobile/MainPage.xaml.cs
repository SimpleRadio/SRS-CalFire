﻿using System.Net;
using CommunityToolkit.Maui.Alerts;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Singleton;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.AircraftRadio;
using Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.Settings;
using CommunityToolkit.Maui.Core;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile;

public partial class MainPage : ContentPage
{
    private SRSAudioManager _srsAudioManager;
    private int count = 0;

    public MainPage()
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

    private void OnStopClicked(object sender, EventArgs e)
    {
        _srsAudioManager?.StopEncoding();
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        _srsAudioManager?.StopEncoding();

        if (IPEndPoint.TryParse(Address.Text, out var result))
        {
            _srsAudioManager = new SRSAudioManager();
            _srsAudioManager.StartPreview(result);
        }
        else
        {
            DisplayAlert("Error", "Invalid IP and port", "OK");
        }
    }

    private bool _isTransitioning = false;
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
            Toast.Make("Loading Handheld Radio", ToastDuration.Short, 14).Show();
            Navigation.PushAsync(new HandheldRadioPage(_srsAudioManager), true);
        }
     
    }

    private void AircraftRadio_OnClicked(object sender, EventArgs e)
    {
        if (!_isTransitioning)
        {
            _isTransitioning = true;
            ClientStateSingleton.Instance.PlayerUnitState.LoadMultiRadio();
            Toast.Make("Loading Aircraft Radio", ToastDuration.Short, 14).Show();
            Navigation.PushAsync(new AircraftRadioPage(_srsAudioManager),true);
        }
    }

    private void Settings_OnClicked(object sender, EventArgs e)
    {
        if (!_isTransitioning)
        {
            _isTransitioning = true;
            Toast.Make("Loading Settings", ToastDuration.Short, 14).Show();
            Navigation.PushAsync(new ClientSettingsPage(), true);
        }
    }
}