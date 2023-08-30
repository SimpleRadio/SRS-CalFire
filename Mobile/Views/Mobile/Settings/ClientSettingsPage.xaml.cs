namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Views.Mobile.Settings;

/// <summary>
///     Interaction logic for ClientSettings.xaml
/// </summary>
public partial class ClientSettingsPage : ContentPage
{
    public ClientSettingsPage()
    {
        InitializeComponent();

        BindingContext = new ClientSettingsViewModel();
    }
}