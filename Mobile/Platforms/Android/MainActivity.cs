using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Ciribob.SRS.Common.Network.Singletons;
using Ciribob.SRS.Mobile.Client;
using Java.Lang;

namespace Mobile
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public MainActivity()
        {
            // TODO convert the audio into a service - communicate over event bus
            //https://stackoverflow.com/questions/71259615/how-to-create-a-background-service-in-net-maui
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                case Keycode.VolumeUp:

                    EventBus.Instance.PublishOnBackgroundThreadAsync(new PTTState() { PTTPressed = true });
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

                    EventBus.Instance.PublishOnBackgroundThreadAsync(new PTTState() { PTTPressed = false });
                    return true;
                    break;
            }
            return base.OnKeyUp(keyCode, e);
        }
    }
}