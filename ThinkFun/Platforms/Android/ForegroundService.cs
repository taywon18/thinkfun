using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
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
    NotificationChannel NotificationChannel;
    int NotificationId = 1000;

    static public ForegroundService LastService = null;

    public ForegroundService()
        : base()
    {
        LastService = this;
        Console.WriteLine("Service recreated");
    }

    public ForegroundService(nint javaref, JniHandleOwnership own)
    : base(javaref, own)
    {
        LastService = this;
        Console.WriteLine("Service recreated");
    }

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

    
    private int NOTIFICATION_ID = 2001;
    private string NOTIFICATION_CHANNEL_ID = "1021";
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
        StartForeground(NOTIFICATION_ID, nxotification.Build());*/

        NotificationChannel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME, NotificationImportance.High);
        NotificationChannel.EnableVibration(true);
        NotificationManager manager = (NotificationManager)MainActivity.ActivityCurrent.GetSystemService(Context.NotificationService);
        manager.CreateNotificationChannel(NotificationChannel);

        /*Intent intent = new Intent(this, typeof(MainActivity));
        const int pendingIntentId = 0;
        PendingIntent pendingIntent = PendingIntent.GetActivity(this, pendingIntentId, intent, PendingIntentFlags.OneShot);*/


        Notification notification = new Notification.Builder(this, NOTIFICATION_CHANNEL_ID)
           .SetContentTitle("FunWatch")
           .SetContentText("L'application reste active en arrière-plan ☺")
           .SetSmallIcon(Resource.Drawable.abc_ab_share_pack_mtrl_alpha)
           //.SetContentIntent(pendingIntent)
           //.SetOngoing(true)
           .Build();

        StartForeground(100, notification);

    }

    public void Notify(string? title, string? content, string? icon)
    {
        var not = new Notification.Builder(this, NOTIFICATION_CHANNEL_ID);
        not.SetSmallIcon(Resource.Drawable.abc_ab_share_pack_mtrl_alpha);

        if (title != null)
            not.SetContentTitle(title);

        if (content != null)
            not.SetContentText(content);

        if (icon != null)
        {
            /*var image = new Image { Source = icon };
            not.SetSmallIcon(Drawable.i)*/
        }
            

        NotificationManager manager = (NotificationManager)MainActivity.ActivityCurrent.GetSystemService(Context.NotificationService);
        manager.Notify(NotificationId++, not.Build());
    }
}
