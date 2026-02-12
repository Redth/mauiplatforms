using Foundation;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.TvOS.Hosting;

namespace Sample;

[Register("MauiTvOSApp")]
public class MauiTvOSApp : TvOSMauiApplication
{
    protected override MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseTvOSMauiApp<App>();
        return builder.Build();
    }
}
