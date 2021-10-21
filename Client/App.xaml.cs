using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using MahApps.Metro.Controls;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Sentry;

namespace SRSClient
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool loggingReady = false;
        private static Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            SentrySdk.Init("https://278831323bbb4efb94e17bc21b5f881d@o414743.ingest.sentry.io/6011780");
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            var location = AppDomain.CurrentDomain.BaseDirectory;
            //var location = Assembly.GetExecutingAssembly().Location;

            //check for opus.dll
            if (!File.Exists(location + "\\opus.dll"))
            {
                MessageBox.Show(
                    $"You are missing the opus.dll - Reinstall using the Installer and don't move the client from the installation directory!",
                    "Installation Error!", MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Environment.Exit(1);
            }

            if (!File.Exists(location + "\\speexdsp.dll"))
            {
                MessageBox.Show(
                    $"You are missing the speexdsp.dll - Reinstall using the Installer and don't move the client from the installation directory!",
                    "Installation Error!", MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Environment.Exit(1);
            }

            SetupLogging();

            ListArgs();


            if (IsClientRunning())
            {
                //check environment flag

                var args = Environment.GetCommandLineArgs();
                var allowMultiple = false;

                foreach (var arg in args)
                    if (arg.Contains("-allowMultiple"))
                        //restart flag to promote to admin
                        allowMultiple = true;

                if (GlobalSettingsStore.Instance.GetClientSettingBool(GlobalSettingsKeys.AllowMultipleInstances) ||
                    allowMultiple)
                {
                    Logger.Warn(
                        "Another SRS instance is already running, allowing multiple instances due to config setting");
                }
                else
                {
                    Logger.Warn("Another SRS instance is already running, preventing second instance startup");

                    var result = MessageBox.Show(
                        "Another instance of the SimpleRadio client is already running!\n\nThis one will now quit. Check your system tray for the SRS Icon",
                        "Multiple SimpleRadio clients started!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);


                    Environment.Exit(0);
                    return;
                }
            }


            InitNotificationIcon();
        }

        private void ListArgs()
        {
            Logger.Info("Arguments:");
            var args = Environment.GetCommandLineArgs();
            foreach (var s in args) Logger.Info(s);
        }


        private bool IsClientRunning()
        {
            var currentProcess = Process.GetCurrentProcess();
            var currentProcessName = currentProcess.ProcessName.ToLower().Trim();

            foreach (var clsProcess in Process.GetProcesses())
                if (clsProcess.Id != currentProcess.Id &&
                    clsProcess.ProcessName.ToLower().Trim() == currentProcessName)
                    return true;

            return false;
        }

        /* 
         * Changes to the logging configuration in this method must be replicated in
         * this VS project's NLog.config file
         */
        private void SetupLogging()
        {
            // If there is a configuration file then this will already be set
            if (LogManager.Configuration != null)
            {
                loggingReady = true;
                return;
            }

            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget
            {
                FileName = "clientlog.txt",
                ArchiveFileName = "clientlog.old.txt",
                MaxArchiveFiles = 1,
                ArchiveAboveSize = 104857600,
                Layout =
                    @"${longdate} | ${logger} | ${message} ${exception:format=toString,Data:maxInnerExceptionLevel=1}"
            };

            var wrapper = new AsyncTargetWrapper(fileTarget, 5000, AsyncTargetWrapperOverflowAction.Discard);
            config.AddTarget("asyncFileTarget", wrapper);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, wrapper));

            LogManager.Configuration = config;
            loggingReady = true;

            Logger = LogManager.GetCurrentClassLogger();
        }


        private void InitNotificationIcon()
        {
            // if (_notifyIcon != null) return;
            // var notifyIconContextMenuShow = new System.Windows.Forms.MenuItem
            // {
            //     Index = 0,
            //     Text = "Show"
            // };
            // notifyIconContextMenuShow.Click += new EventHandler(NotifyIcon_Show);
            //
            // var notifyIconContextMenuQuit = new System.Windows.Forms.MenuItem
            // {
            //     Index = 1,
            //     Text = "Quit"
            // };
            // notifyIconContextMenuQuit.Click += new EventHandler(NotifyIcon_Quit);
            //
            // var notifyIconContextMenu = new System.Windows.Forms.ContextMenu();
            // notifyIconContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
            //     { notifyIconContextMenuShow, notifyIconContextMenuQuit });
            //
            // _notifyIcon = new System.Windows.Forms.NotifyIcon
            // {
            //     Icon = Ciribob.FS3D.SimpleRadio.Standalone.Client.Properties.Resources.audio_headset,
            //     Visible = true
            // };
            // _notifyIcon.ContextMenu = notifyIconContextMenu;
            // _notifyIcon.DoubleClick += new EventHandler(NotifyIcon_Show);
        }

        private void NotifyIcon_Show(object sender, EventArgs args)
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
        }

        private void NotifyIcon_Quit(object sender, EventArgs args)
        {
            MainWindow.Close();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //TODO fix this
            // if (_notifyIcon != null)
            //     _notifyIcon.Visible = false;
            base.OnExit(e);
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (loggingReady)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Error((Exception)e.ExceptionObject, "Received unhandled exception, {0}",
                    e.IsTerminating ? "exiting" : "continuing");
            }
        }
    }
}