using System.Windows.Controls;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.Common.PresetChannels
{
    /// <summary>
    ///     Interaction logic for PresetChannelsView.xaml
    /// </summary>
    public partial class PresetChannelsView : UserControl
    {
        public PresetChannelsView()
        {
            InitializeComponent();

            //set to window width
            FrequencyDropDown.Width = Width;
        }
    }
}