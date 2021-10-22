using System;
using System.Windows.Data;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI;
using Ciribob.SRS.Common.Network.Singletons;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils.ValueConverters
{
    internal class ConnectionStatusImageConverter : IValueConverter
    {
        private ClientStateSingleton _clientState { get; } = ClientStateSingleton.Instance;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var connected = (bool)value;
            if (connected)
                return Images.IconConnected;
            else
                return Images.IconDisconnected;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}