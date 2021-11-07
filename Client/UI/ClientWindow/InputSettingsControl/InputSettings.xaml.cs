using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.ClientWindow.InputSettingsControl
{
    /// <summary>
    /// Interaction logic for InputSettings.xaml
    /// </summary>
    public partial class InputSettings : UserControl
    {
        public InputSettings()
        {
            InitializeComponent();
        }

        private void Rescan_OnClick(object sender, RoutedEventArgs e)
        {
            InputDeviceManager.Instance.InitDevices();
        }
    }
}
