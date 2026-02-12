using Foundation;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.MacOS.Hosting;

namespace Sample;

[Register("MauiMacOSApp")]
public class MauiMacOSApp : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMacOSMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        return builder.Build();
    }
}
