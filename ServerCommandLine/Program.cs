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
            int port = 5002;
            bool recording = false;
            string frequencies = "";
            foreach (var arg in args)
            {
                if (arg.Trim().StartsWith("--port="))
                {
                    string temp = arg.Trim().Replace("--port=", "");
                    
                    port = int.Parse(temp.Trim());
                }
                
                if (arg.Trim().StartsWith("--recording="))
                {
                    string temp = arg.Trim().Replace("--recording=", "");
                    
                    recording = bool.Parse(temp.Trim());
                }
                
                if (arg.Trim().StartsWith("--recordingFreqs="))
                {
                    frequencies = arg.Trim().Replace("--recordingFreqs=", "");
                }
            }

            Console.WriteLine($"Using Port {port} and recording {recording}");
            
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Program p = new Program();
            new Thread(() =>
            {
                p.StartServer(port, recording, frequencies);
            }).Start();


            var waitForProcessShutdownStart = new ManualResetEventSlim();
            var waitForMainExit = new ManualResetEventSlim();

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                // We got a SIGTERM, signal that graceful shutdown has started
                waitForProcessShutdownStart.Set();

                Console.WriteLine("Shutting down gracefully...");
                // Don't unwind until main exists
                waitForMainExit.Wait();
            };

            Console.WriteLine("Waiting for shutdown SIGTERM");
            // Wait for shutdown to start
            waitForProcessShutdownStart.Wait();

            // This is where the application performs graceful shutdown
            p.StopServer();

            Console.WriteLine("Shutdown complete");
            // Now we're done with main, tell the shutdown handler
            waitForMainExit.Set();

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


        public void StartServer(int port, bool recording, string frequencies)
        {
            _serverState = new ServerState(_eventAggregator, port,true, recording, frequencies);
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