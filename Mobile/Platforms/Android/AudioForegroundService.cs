using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Caliburn.Micro;
using Ciribob.SRS.Common.Network.Models.EventMessages;
using Ciribob.SRS.Common.Network.Singletons;
using Application = Android.App.Application;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Platforms.Android;

/// <summary>
///     Just by having this service active it keeps our app running and the audio playing correctly
/// </summary>
[Service]
internal class AudioForegroundService : Service, IHandle<TCPClientStatusMessage>
{
    private PowerManager.WakeLock _wakeLock;
    private readonly string NOTIFICATION_CHANNEL_ID = "1801";
    private readonly string NOTIFICATION_CHANNEL_NAME = "fs3d";
    private readonly int NOTIFICATION_ID = 1;

    public Task HandleAsync(TCPClientStatusMessage message, CancellationToken cancellationToken)
    {
        //todo
        //UpdateNotification()

        if (!message.Connected)
        {
            ReturnWakeLock();
            //terminate
            StopSelf();
        }

        return Task.CompletedTask;
    }

    private void StartForegroundService()
    {
        var notifcationManager = GetSystemService(NotificationService) as NotificationManager;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O) createNotificationChannel(notifcationManager);

        StartForeground(NOTIFICATION_ID, GenerateNotification());
        EventBus.Instance.SubscribeOnUIThread(this);

        GetWakeLock();
    }

    private void GetWakeLock()
    {
        _wakeLock?.Release();

        var wakeFlags = WakeLockFlags.Partial;

        var pm =
            (PowerManager)Application.Context.GetSystemService(PowerService);
        _wakeLock = pm.NewWakeLock(wakeFlags, typeof(AudioForegroundService).FullName);
        _wakeLock?.Acquire();
    }

    private void ReturnWakeLock()
    {
        _wakeLock?.Release();
    }

    private Notification GenerateNotification()
    {
        var intent = new Intent(Platform.AppContext, typeof(MainActivity));
        var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.UpdateCurrent |
              PendingIntentFlags.Mutable
            : PendingIntentFlags.UpdateCurrent;
        var pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, intent, pendingIntentFlags);

        var status = SRSConnectionManager.Instance;
        var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
        notification.SetAutoCancel(false);
        notification.SetOngoing(true);
        notification.SetSmallIcon(Resource.Mipmap.appicon);
        notification.SetContentTitle("FS3D");
        notification.SetContentText("FS3D is running");
        notification.SetContentIntent(pendingIntent);
        return notification.Build();
    }

    private void createNotificationChannel(NotificationManager notificationMnaManager)
    {
        var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME,
            NotificationImportance.Low);
        notificationMnaManager.CreateNotificationChannel(channel);
    }

    public override IBinder OnBind(Intent intent)
    {
        return null;
    }

    // private void UpdateNotification() {
    //     String text = "Some text that will update the notification";
    //
    //     Notification notification = GenerateNotification();
    //
    //     var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;
    //     notifcationManager.Notify(NOTIFICATION_ID, notification);
    // }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        StartForegroundService();
        return StartCommandResult.NotSticky;
    }

    public override void OnDestroy()
    {
        EventBus.Instance.Unsubcribe(this);
        base.OnDestroy();
    }
}