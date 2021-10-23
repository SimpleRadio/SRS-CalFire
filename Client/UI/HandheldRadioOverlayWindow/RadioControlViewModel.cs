using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons.Models;
using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.HandheldRadioOverlayWindow
{
    public class RadioControlViewModel
    {
        public RadioControlViewModel(int radioId)
        {
            RadioId = radioId;
        }
        //TODO fill out buttons here
        public SolidColorBrush RadioActiveFill
        {
            get
            {
                if (Radio == null || Radio.Modulation == Modulation.DISABLED)
                {
                    return new SolidColorBrush(Colors.Red);
                }

                if (ClientStateSingleton.Instance.PlayerUnitState.SelectedRadio != RadioId)
                {
                    return new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    if (ClientStateSingleton.Instance.RadioSendingState.IsSending &&
                        ClientStateSingleton.Instance.RadioSendingState.SendingOn == RadioId)
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#96FF6D"));
                    }
                    else
                    {
                        return new SolidColorBrush(Colors.Green);
                    }
                }

                return new SolidColorBrush(Colors.Red);
            }
        }

        public int RadioId { get; set; }

        public Radio Radio
        {
            get
            {
                return ClientStateSingleton.Instance.PlayerUnitState.Radios[RadioId];
            }
        }

    }
}
