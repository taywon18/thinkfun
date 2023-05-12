using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace ThinkFun;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp(true)
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        #if ANDROID
            Android.Content.Intent intent = new Android.Content.Intent(Android.App.Application.Context, typeof(ThinkFun.Platforms.Android.ForegroundService));
            Android.App.Application.Context.StartForegroundService(intent);
            Android.App.Application.Context.StopService(intent);
        #endif

        return builder.Build();
    }
}