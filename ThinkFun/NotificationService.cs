using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkFun
{
    public class NotificationService
    {
        public static NotificationService Instance { get; } = new();
        bool Inited = false;
        #if ANDROID
            Android.Content.Intent AndroidIntend = null;
        #endif



        public void Init()
        {
            if(Inited) return;
            Inited = true;

            #if ANDROID
                AndroidIntend = new Android.Content.Intent(Android.App.Application.Context, typeof(ThinkFun.Platforms.Android.ForegroundService));
                Android.App.Application.Context.StartForegroundService(AndroidIntend);
                Console.WriteLine("Foreground service created.");
            #endif
        }

        public void Stop()
        {
            #if ANDROID
                Android.App.Application.Context.StopService(AndroidIntend);
            #endif
        }

        public void Notify(string? title, string? content)
        {
            #if ANDROID
                ThinkFun.Platforms.Android.ForegroundService.LastService.Notify(title, content);
            #endif
        }
    }
}
