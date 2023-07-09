using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        // Add NLog for Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddNLog();
        
#if DEBUG
        var logger = NLog.LogManager.Setup().RegisterMauiLog()
            .LoadConfiguration(c => c.ForLogger(NLog.LogLevel.Debug).WriteToMauiLog())
            .GetCurrentClassLogger();
#else
        var logger = NLog.LogManager.Setup().RegisterMauiLog()
            .LoadConfiguration(c => c.ForLogger(NLog.LogLevel.Info).WriteToMauiLog())
            .GetCurrentClassLogger();
#endif

        return builder.Build();
    }
}