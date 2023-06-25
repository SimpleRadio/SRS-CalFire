using Ciribob.FS3D.SimpleRadio.Standalone.Mobile;

namespace Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}