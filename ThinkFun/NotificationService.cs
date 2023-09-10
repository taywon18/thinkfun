using Microsoft.Maui.Controls.PlatformConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkFun;

public class NotificationService
{
    static public NotificationService Instance { get; } = new NotificationService();

#if ANDROID
    Android.Content.Intent Intend;
#endif

    public bool IsWorking
    {
        get
        {
            #if ANDROID
                return Intend != null;
            #elif WINDOWS
                return false; //TODO: Implement
            #endif
            throw new NotImplementedException();
        }
    }

    public void StartWorkingBackground()
    {
#if ANDROID
        if(Intend != null)
            StopWorkingBackground();

        Intend = new Android.Content.Intent(Android.App.Application.Context, typeof(ThinkFun.Platforms.Android.ForegroundService));
        Android.App.Application.Context.StartForegroundService(Intend);  
#endif
    }

    public void StopWorkingBackground()
    {
#if ANDROID
        Android.Content.Intent intent = new Android.Content.Intent(Android.App.Application.Context, typeof(ThinkFun.Platforms.Android.ForegroundService));
            intent.SetAction("STOP_SERVICE");
         MainActivity.ActivityCurrent.StartService(intent);
         Intend = null;
#endif
    }

    public void Notify(string? title, string? content, string? icon = null)
    {
#if ANDROID
                ThinkFun.Platforms.Android.ForegroundService.LastService.Notify(title, content, icon);
#endif
    }
}
