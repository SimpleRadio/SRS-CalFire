using System.Runtime;
using Caliburn.Micro;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Network;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Settings;
using CommandLine;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Sentry;
using LogManager = NLog.LogManager;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server;

internal class Program
{
    private readonly EventAggregator _eventAggregator = new();
    private ServerState _serverState;

    public Program()
    {
        SentrySdk.Init("https://ab4791abe55a4ae384a5a802c3060bc4@o414743.ingest.sentry.io/6011651");
        SetupLogging();
    }

    private static void Main(string[] args)
    {
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(ProcessArgs);
    }

    private static void ProcessArgs(Options options)
    {
        Console.WriteLine($"Settings: \n{options}");

        var p = new Program();
        new Thread(() => { p.StartServer(options); }).Start();

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


    public void StartServer(Options options)
    {
        ServerSettingsStore.Instance.SetServerSetting(ServerSettingsKeys.SERVER_PORT, options.Port);

        if (!string.IsNullOrEmpty(options.RecordingFrequencies))
        {
            ServerSettingsStore.Instance.SetServerSetting(ServerSettingsKeys.SERVER_RECORDING, true);
            ServerSettingsStore.Instance.SetGeneralSetting(ServerSettingsKeys.SERVER_RECORDING_FREQUENCIES,
                options.RecordingFrequencies);
        }
        else
        {
            ServerSettingsStore.Instance.SetServerSetting(ServerSettingsKeys.SERVER_RECORDING, false);
        }

        var profileSettings = GlobalSettingsStore.Instance.ProfileSettingsStore;

        profileSettings.SetClientSettingBool(ProfileSettingsKeys.NATOTone, options.FMRadioTone);
        profileSettings.SetClientSettingBool(ProfileSettingsKeys.RadioEffects, options.RadioEffect);
        profileSettings.SetClientSettingBool(ProfileSettingsKeys.RadioEffectsClipping, options.RadioEffect);
        profileSettings.SetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume, options.FMRadioToneVolume);

        profileSettings.SetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume, options.FMRadioEffectVolume);
        profileSettings.SetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume, options.HFRadioEffectVolume);
        profileSettings.SetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume, options.UHFRadioEffectVolume);
        profileSettings.SetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume, options.VHFRadioEffectVolume);

        profileSettings.SetClientSettingBool(ProfileSettingsKeys.RadioEffects, options.RadioEffect);

        profileSettings.SetClientSettingBool(ProfileSettingsKeys.RadioBackgroundNoiseEffect,
            options.RadioBackgroundEffects);

        profileSettings.SetClientSettingFloat(ProfileSettingsKeys.AircraftNoiseVolume,
            options.AircraftRadioEffectVolume);
        profileSettings.SetClientSettingFloat(ProfileSettingsKeys.GroundNoiseVolume, options.GroundRadioEffectVolume);

        _serverState = new ServerState(_eventAggregator, options.SessionId);
    }

    private void SetupLogging()
    {
        // If there is a configuration file then this will already be set
        if (LogManager.Configuration != null) return;

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

public class Options
{
    [Option('p', "port",
        HelpText = "Port - 5002 is the default",
        Default = 5002,
        Required = false)]
    public int Port { get; set; }

    [Option('f', "recordingFreqs",
        HelpText = "Frequency in MHz comma separated 121.5,122.3,152 - if set, recording is enabled ",
        Required = false)]
    public string RecordingFrequencies { get; set; }

    [Option("audioClippingEffect",
        HelpText =
            "Audio Clipping Effect",
        Default = true,
        Required = false)]
    public bool RadioClipping { get; set; }

    [Option("radioEffect",
        HelpText =
            "Radio Effect - if not enabled other effects are ignored",
        Default = true,
        Required = false)]
    public bool RadioEffect { get; set; }

    [Option("fmRadioTone",
        HelpText =
            "FM Radio Tone",
        Default = true,
        Required = false)]
    public bool FMRadioTone { get; set; }

    [Option("fmRadioToneVolume",
        HelpText =
            "FM Radio Tone Volume",
        Default = 1.2f,
        Required = false)]
    public float FMRadioToneVolume { get; set; }

    [Option("radioBackgroundEffects",
        HelpText =
            "Background radio effects - UHF/VHF/HF and background Aircraft or ground noise",
        Default = true,
        Required = false)]
    public bool RadioBackgroundEffects { get; set; }

    [Option("uhfRadioEffectVolume",
        HelpText =
            "UHF Radio Effect Volume",
        Default = 0.15f,
        Required = false)]
    public float UHFRadioEffectVolume { get; set; }

    [Option("vhfRadioEffectVolume",
        HelpText =
            "VHF Radio Effect Volume",
        Default = 0.15f,
        Required = false)]
    public float VHFRadioEffectVolume { get; set; }

    [Option("hfRadioEffectVolume",
        HelpText =
            "HF Radio Effect Volume",
        Default = 0.15f,
        Required = false)]
    public float HFRadioEffectVolume { get; set; }

    [Option("fmRadioEffectVolume",
        HelpText =
            "FM Radio Effect Volume",
        Default = 0.4f,
        Required = false)]
    public float FMRadioEffectVolume { get; set; }

    [Option("aircraftRadioEffectVolume",
        HelpText =
            "Aircraft Radio Effect Volume",
        Default = 0.5f,
        Required = false)]
    public float AircraftRadioEffectVolume { get; set; }

    [Option("groundRadioEffectVolume",
        HelpText =
            "Ground Radio Effect Volume",
        Default = 0.5f,
        Required = false)]
    public float GroundRadioEffectVolume { get; set; }

    [Option("sessionId",
        HelpText =
            "Session ID",
        Required = true)]
    public string SessionId { get; set; }

    public override string ToString()
    {
        return
            $"{nameof(SessionId)}: {SessionId}, \n" +
            $"{nameof(RecordingFrequencies)}: {RecordingFrequencies}, \n" +
            $"{nameof(RadioClipping)}: {RadioClipping}, \n" +
            $"{nameof(RadioEffect)}: {RadioEffect}, \n" +
            $"{nameof(FMRadioTone)}: {FMRadioTone}, \n" +
            $"{nameof(FMRadioToneVolume)}: {FMRadioToneVolume}, \n" +
            $"{nameof(RadioBackgroundEffects)}: {RadioBackgroundEffects}, \n" +
            $"{nameof(UHFRadioEffectVolume)}: {UHFRadioEffectVolume}, \n" +
            $"{nameof(VHFRadioEffectVolume)}: {VHFRadioEffectVolume}, \n" +
            $"{nameof(HFRadioEffectVolume)}: {HFRadioEffectVolume}, \n" +
            $"{nameof(FMRadioEffectVolume)}: {FMRadioEffectVolume}, \n" +
            $"{nameof(AircraftRadioEffectVolume)}: {AircraftRadioEffectVolume}, \n" +
            $"{nameof(GroundRadioEffectVolume)}: {GroundRadioEffectVolume}";
    }
}