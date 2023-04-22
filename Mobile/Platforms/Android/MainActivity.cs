using Android.App;
using Android.Content.PM;
using Android.Views;
using Ciribob.SRS.Common.Network.Singletons;
using Ciribob.SRS.Mobile.Client;

namespace Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
    {
        switch (keyCode)
        {
            case Keycode.VolumeUp:

                EventBus.Instance.PublishOnBackgroundThreadAsync(new PTTState { PTTPressed = true });
                return true;
                break;
        }

        return base.OnKeyDown(keyCode, e);
    }

    public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
    {
        switch (keyCode)
        {
            case Keycode.VolumeUp:

                EventBus.Instance.PublishOnBackgroundThreadAsync(new PTTState { PTTPressed = false });
                return true;
                break;
        }

        return base.OnKeyUp(keyCode, e);
    }
}