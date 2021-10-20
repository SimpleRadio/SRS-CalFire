using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Ciribob.FS3D.SimpleRadio.Standalone.Client;
using Ciribob.SRS.Client.Network;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.RadioOverlayWindow;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings.RadioChannels;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.Common.PresetChannels;
using NLog;
using Ciribob.SRS.Common;
using Ciribob.SRS.Common.PlayerState;
using Newtonsoft.Json;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Overlay
{
    /// <summary>
    ///     Interaction logic for RadioOverlayWindow.xaml
    /// </summary>
    public partial class RadioOverlayWindow : Window
    {
        private  double _aspectRatio;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Client.UI.RadioOverlayWindow.RadioControlGroup radio;

        private readonly DispatcherTimer _updateTimer;

        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

        private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;

        private readonly double _originalMinHeight;

        private static readonly string HANDHELD_RADIO_JSON = "handheld-radio.json";

        public RadioOverlayWindow()
        {
            LoadHandheldRadioConfig();
            //load opacity before the intialising as the slider changed
            //method fires after initialisation
            InitializeComponent();

      

            this.WindowStartupLocation = WindowStartupLocation.Manual;

            _aspectRatio = MinWidth / MinHeight;
            _originalMinHeight = MinHeight;

            AllowsTransparency = true;
            Opacity = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioOpacity).DoubleValue;
            WindowOpacitySlider.Value = Opacity;

            radio = Radio1;

            //allows click and drag anywhere on the window
            ContainerPanel.MouseLeftButtonDown += WrapPanel_MouseLeftButtonDown;

            Left = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioX).DoubleValue;
            Top = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioY).DoubleValue;

            Width = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioWidth).DoubleValue;
            Height = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioHeight).DoubleValue;

            //  Window_Loaded(null, null);
            CalculateScale();

            LocationChanged += Location_Changed;

            RadioRefresh(null, null);

            //init radio refresh
            _updateTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(80)};
            _updateTimer.Tick += RadioRefresh;
            _updateTimer.Start();

            


            //TODO on loading the overlay
            //load the handheld-radio.json
            //load the fixed channel files
            //send the new radio channel config (use the singleton to co-ordinate)
            //on closing the overlay, set all the radios to disabled
        }

        private void LoadHandheldRadioConfig()
        {
            Radio[] handheldRadio;
            try
            {
                string radioJson = File.ReadAllText(HANDHELD_RADIO_JSON);
                handheldRadio = JsonConvert.DeserializeObject<Radio[]>(radioJson);

                if (handheldRadio.Length < 2)
                {
                    throw new Exception("Not enough radios configured");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load "+HANDHELD_RADIO_JSON);

                handheldRadio = new Radio[11];
                for (int i = 0; i < 11; i++)
                {
                    handheldRadio[i] = new Radio
                    {
                        Config = new RadioConfig()
                        {
                            MinimumFrequency = 1,
                            MaxFrequency = 1,
                            FrequencyControl = RadioConfig.FreqMode.COCKPIT,
                            VolumeControl = RadioConfig.VolumeMode.COCKPIT,
                        },
                        Frequency = 1,
                        SecondaryFrequency = 0,
                        Modulation = RadioConfig.Modulation.DISABLED,
                        Name = "Invalid Config",
                    };
                }

                handheldRadio[1] =  new Radio
                {
                    Frequency = 1.51e+8,
                    Config = new RadioConfig()
                    {
                        MinimumFrequency = 1.0e+8,
                        MaxFrequency = 3.51e+8,
                        FrequencyControl = RadioConfig.FreqMode.OVERLAY,
                        VolumeControl = RadioConfig.VolumeMode.OVERLAY,
                    },
                    SecondaryFrequency = 1.215e+8,
                    Modulation = RadioConfig.Modulation.AM,
                    Name = "BK RADIO",
                };
            }

            ClientStateSingleton.Instance.PlayerUnitState.Radios = new List<Radio>(handheldRadio);

            // Force an immediate update of radio information
            _clientStateSingleton.LastSent = 0;


            //load handheld-radio.json

            var fixedChannels = _clientStateSingleton.FixedChannels;
            
            for (var i = 1; i < ClientStateSingleton.Instance.PlayerUnitState.Radios.Count; i++)
            {
                fixedChannels[i-1] = new PresetChannelsViewModel(new FilePresetChannelsStore(), i);
            }
        }

        private void Location_Changed(object sender, EventArgs e)
        {
        }

        private void RadioRefresh(object sender, EventArgs eventArgs)
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.PlayerUnitState;

            radio.RepaintRadioStatus();
            radio.RepaintRadioReceive();

            if ((dcsPlayerRadioInfo != null))
            {
                var availableRadios = 0;
                if (MinHeight != _originalMinHeight)
                {
                    MinHeight = _originalMinHeight;
                    Recalculate();
                }
            }
        }

        private void Recalculate()
        {
            _aspectRatio = MinWidth / MinHeight;
            containerPanel_SizeChanged(null, null);
            Height = Height+1;
        }

        private void WrapPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioWidth, Width);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioHeight,Height);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioOpacity,Opacity);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioX,Left);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioY, Top);
            base.OnClosing(e);

            _updateTimer.Stop();
        }

        private void Button_Minimise(object sender, RoutedEventArgs e)
        {
            // Minimising a window without a taskbar icon leads to the window's menu bar still showing up in the bottom of screen
            // Since controls are unusable, but a very small portion of the always-on-top window still showing, we're closing it instead, similar to toggling the overlay
            if (_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide))
            {
                Close();
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
        }

      

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void windowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Opacity = e.NewValue;
        }

        private void containerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //force aspect ratio
            CalculateScale();

            WindowState = WindowState.Normal;
        }


        private void CalculateScale()
        {
            var yScale = ActualHeight / RadioOverlayWin.MinWidth;
            var xScale = ActualWidth / RadioOverlayWin.MinWidth;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double) OnCoerceScaleValue(RadioOverlayWin, value);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged)
                Width = sizeInfo.NewSize.Height * _aspectRatio;
            else
                Height = sizeInfo.NewSize.Width / _aspectRatio;


            // Console.WriteLine(this.Height +" width:"+ this.Width);
        }

        #region ScaleValue Depdency Property //StackOverflow: http://stackoverflow.com/questions/3193339/tips-on-developing-resolution-independent-application/5000120#5000120

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue",
            typeof(double), typeof(RadioOverlayWindow),
            new UIPropertyMetadata(1.0, OnScaleValueChanged,
                OnCoerceScaleValue));


        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            var mainWindow = o as RadioOverlayWindow;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double) value);
            return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var mainWindow = o as RadioOverlayWindow;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double) e.OldValue, (double) e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue)
        {
        }

        public double ScaleValue
        {
            get { return (double) GetValue(ScaleValueProperty); }
            set { SetValue(ScaleValueProperty, value); }
        }

        #endregion

        private void RadioOverlayWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {

            try
            {

                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
            catch (Exception ex)
            {
                //can throw an error if its somehow caused when the left mouse button isnt down
            }

        }
    }
}