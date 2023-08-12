using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkFun.Platforms.Android;

[Service]
public class ForegroundService
    : Service
    , IBackgroundWorker
{
    public override IBinder OnBind(Intent intent)
    {
        throw new NotImplementedException();
    }

    public override void OnCreate()
    {
        base.OnCreate();
        startForegroundService();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        StopForeground(true);//Stop the service
    }

    [return: GeneratedEnum]//we catch the actions intents to know the state of the foreground service
    public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        if (intent.Action == "START_SERVICE")
        {
            //RegisterNotification();//Proceed to notify
            startForegroundService();
        }
        else if (intent.Action == "STOP_SERVICE")
        {
            StopForeground(true);//Stop the service
            StopSelfResult(startId);
        }

        return StartCommandResult.NotSticky;
    }

    
    private int NOTIFICATION_ID = 2;
    private string NOTIFICATION_CHANNEL_ID = "1001";
    private string NOTIFICATION_CHANNEL_NAME = "notification";

    private void startForegroundService()
    {
        /*var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            createNotificationChannel(notifcationManager);
        }

        var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
        notification.SetAutoCancel(false);
        notification.SetOngoing(true);
        notification.SetSmallIcon(Resource.Mipmap.appicon);
        notification.SetContentTitle("ThinkFun2");
        notification.SetContentText("ThinkFun reste actif en arrière plan.");
        //notification.AddAction(new NotificationCompat.Action(null, "Activer", null));
        StartForeground(NOTIFICATION_ID, notification.Build());*/

        NotificationChannel channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME, NotificationImportance.Max);
        NotificationManager manager = (NotificationManager)MainActivity.ActivityCurrent.GetSystemService(Context.NotificationService);
        manager.CreateNotificationChannel(channel);
        Notification notification = new Notification.Builder(this, NOTIFICATION_CHANNEL_ID)
           //.SetContentTitle("Content title")
           .SetContentText("L'application reste active en arrière-plan...")
           .SetSmallIcon(Resource.Drawable.abc_ab_share_pack_mtrl_alpha)
           .SetOngoing(true)
           .Build();

        StartForeground(NOTIFICATION_ID, notification);
    }
}
