using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using NLog;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.AircraftOverlayWindow
{
    /// <summary>
    ///     Interaction logic for RadioOverlayWindow.xaml
    /// </summary>
    public partial class RadioOverlayWindow : Window
    {
        private readonly double _aspectRatio;


        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

        private readonly DispatcherTimer _updateTimer;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly RadioControlGroup[] radioControlGroup = new RadioControlGroup[10];

        private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;

        public RadioOverlayWindow()
        {
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = _globalSettings.GetPositionSetting(GlobalSettingsKeys.AwacsX).DoubleValue;
            Top = _globalSettings.GetPositionSetting(GlobalSettingsKeys.AwacsY).DoubleValue;

            _aspectRatio = MinWidth / MinHeight;

            AllowsTransparency = true;
            //    Opacity = opacity;
            windowOpacitySlider.Value = Opacity;

            radioControlGroup[0] = radio1;
            radioControlGroup[1] = radio2;
            radioControlGroup[2] = radio3;
            radioControlGroup[3] = radio4;
            radioControlGroup[4] = radio5;
            radioControlGroup[5] = radio6;
            radioControlGroup[6] = radio7;
            radioControlGroup[7] = radio8;
            radioControlGroup[8] = radio9;
            radioControlGroup[9] = radio10;


            //allows click and drag anywhere on the window
            containerPanel.MouseLeftButtonDown += WrapPanel_MouseLeftButtonDown;

            //      Top = AppConfiguration.Instance.RadioX;
            //        Left = AppConfiguration.Instance.RadioY;

            //     Width = AppConfiguration.Instance.RadioWidth;
            //      Height = AppConfiguration.Instance.RadioHeight;

            //  Window_Loaded(null, null);

            CalculateScale();

            LocationChanged += Location_Changed;

            RadioRefresh(null, null);

            //init radio refresh
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _updateTimer.Tick += RadioRefresh;
            _updateTimer.Start();


            //TODO on loading the overlay
            //load the aircraft-radio.json
            //load the fixed channel files
            //send the new radio channel config (use the singleton to co-ordinate)
            //on closing the overlay, set all the radios to disabled
            //close the overlay if the server is not connected? how to keep in sync with the server?
        }

        private void Location_Changed(object sender, EventArgs e)
        {
            //   AppConfiguration.Instance.RadioX = Top;
            //  AppConfiguration.Instance.RadioY = Left;
        }

        private void RadioRefresh(object sender, EventArgs eventArgs)
        {
            foreach (var radio in radioControlGroup)
            {
                radio.RepaintRadioStatus();
                radio.RepaintRadioReceive();
            }

            var playerRadioInfo = _clientStateSingleton.PlayerUnitState;

            //TODO fix
            // if (playerRadioInfo != null)
            //     if (_clientStateSingleton.IsConnected)
            //         ToggleGlobalSimultaneousTransmissionButton.IsEnabled = true;
        }

        private void WrapPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.AwacsX, Left);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.AwacsY, Top);

            base.OnClosing(e);

            _updateTimer.Stop();
        }

        private void Button_Minimise(object sender, RoutedEventArgs e)
        {
            // Minimising a window without a taskbar icon leads to the window's menu bar still showing up in the bottom of screen
            // Since controls are unusable, but a very small portion of the always-on-top window still showing, we're closing it instead, similar to toggling the overlay
            if (_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide))
                Close();
            else
                WindowState = WindowState.Minimized;
        }


        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void windowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Opacity = e.NewValue;
            //AppConfiguration.Instance.RadioOpacity = Opacity;
        }

        private void containerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //force aspect ratio
            CalculateScale();

            WindowState = WindowState.Normal;
        }

//
//
        private void CalculateScale()
        {
            var yScale = ActualHeight / RadioOverlayWin.MinWidth;
            var xScale = ActualWidth / RadioOverlayWin.MinWidth;
            var value = Math.Max(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(RadioOverlayWin, value);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged)
                Width = sizeInfo.NewSize.Height * _aspectRatio;
            else
                Height = sizeInfo.NewSize.Width / _aspectRatio;

            //  AppConfiguration.Instance.RadioWidth = Width;
            // AppConfiguration.Instance.RadioHeight = Height;
            // Console.WriteLine(this.Height +" width:"+ this.Width);
        }

        private void ToggleGlobalSimultaneousTransmissionButton_Click(object sender, RoutedEventArgs e)
        {
            // var dcsPlayerRadioInfo = _clientStateSingleton.PlayerUnitState;
            // if (dcsPlayerRadioInfo != null)
            // {
            //     dcsPlayerRadioInfo.simultaneousTransmission = !dcsPlayerRadioInfo.simultaneousTransmission;
            //
            //     if (!dcsPlayerRadioInfo.simultaneousTransmission)
            //         foreach (var radio in dcsPlayerRadioInfo.Radios)
            //             radio.SimultaneousTransmission = false;
            //
            //     ToggleGlobalSimultaneousTransmissionButton.Content =
            //         _clientStateSingleton.PlayerUnitState.simultaneousTransmission
            //             ? "Simul. Transmission ON"
            //             : "Simul. Transmission OFF";
            //     ToggleGlobalSimultaneousTransmissionButton.Foreground =
            //         _clientStateSingleton.PlayerUnitState.simultaneousTransmission
            //             ? new SolidColorBrush(Colors.Orange)
            //             : new SolidColorBrush(Colors.White);
            //
            //     foreach (var radio in radioControlGroup)
            //     {
            //         if (!dcsPlayerRadioInfo.simultaneousTransmission)
            //             radio.ToggleSimultaneousTransmissionButton.Foreground = new SolidColorBrush(Colors.White);
            //
            //         radio.RepaintRadioStatus();
            //     }
            // }
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
                return mainWindow.OnCoerceScaleValue((double)value);
            return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var mainWindow = o as RadioOverlayWindow;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
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
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }

        #endregion
    }
}