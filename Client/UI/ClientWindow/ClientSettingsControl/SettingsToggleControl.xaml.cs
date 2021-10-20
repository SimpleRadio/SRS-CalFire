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

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl
{
    /// <summary>
    /// Interaction logic for SettingsToggleControl.xaml
    /// </summary>
    public partial class SettingsToggleControl : UserControl
    {
        public SettingsToggleControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ToggleDependencyProperty =
            DependencyProperty.Register("ToggleValue", typeof(bool), typeof(SettingsToggleControl),
                new FrameworkPropertyMetadata((bool)false)
            );

        public bool ToggleValue
        {
            set => SetValue(ToggleDependencyProperty, value);
            get => (bool) GetValue(ToggleDependencyProperty);
        }
    }
}
