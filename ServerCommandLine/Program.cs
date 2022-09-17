using System.Runtime;
using System.Threading;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Network;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Sentry;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server
{
    internal class Program
    {
        private ServerState _serverState;
        private EventAggregator _eventAggregator = new EventAggregator();
        static void Main(string[] args)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Program p = new Program();
            new Thread(() =>
            {
                p.StartServer();
            }).Start();


            while (true)
            {
                System.Console.WriteLine("Server Running.... Type quit to terminate");
                var line = System.Console.ReadLine();
                if (line != null && line.Trim().ToLowerInvariant() == "quit")
                {
                    p.StopServer();
                    System.Console.WriteLine("Closing...");
                    return;
                }
            }
        }

        private void StopServer()
        {
            _serverState.StopServer();
        }

        public Program()
        {
            SentrySdk.Init("https://ab4791abe55a4ae384a5a802c3060bc4@o414743.ingest.sentry.io/6011651");
            SetupLogging();
        }


        public void StartServer()
        {
            _serverState = new ServerState(_eventAggregator);
        }

        private void SetupLogging()
        {
            // If there is a configuration file then this will already be set
            if (LogManager.Configuration != null)
            {
                return;
            }

            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget
            {
                FileName = "serverlog.txt",
                ArchiveFileName = "serverlog.old.txt",
                MaxArchiveFiles = 1,
                ArchiveAboveSize = 104857600,
                Layout =
                    @"${longdate} | ${logger} | ${message} ${exception:format=toString,Data:maxInnerExceptionLevel=1}"
            };

            var wrapper = new AsyncTargetWrapper(fileTarget, 5000, AsyncTargetWrapperOverflowAction.Discard);
            config.AddTarget("asyncFileTarget", wrapper);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, wrapper));

            LogManager.Configuration = config;
        }
    }
}