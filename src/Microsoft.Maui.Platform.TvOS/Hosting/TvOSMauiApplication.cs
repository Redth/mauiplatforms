using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.TvOS.Dispatching;
using Microsoft.Maui.Platform.TvOS.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Hosting;

[Register("TvOSMauiApplication")]
public abstract class TvOSMauiApplication : UIApplicationDelegate, IPlatformApplication
{
    MauiApp? _mauiApp;
    TvOSMauiContext? _mauiContext;
    IApplication? _application;

    public IServiceProvider Services => _mauiApp?.Services ?? throw new InvalidOperationException("MauiApp not initialized");

    IApplication IPlatformApplication.Application => _application ?? throw new InvalidOperationException("Application not initialized");

    public TvOSMauiContext MauiContext => _mauiContext ?? throw new InvalidOperationException("MauiContext not initialized");

    public override UIWindow? Window { get; set; }

    protected abstract MauiApp CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        try
        {
            _mauiApp = CreateMauiApp();
            _mauiContext = new TvOSMauiContext(_mauiApp.Services);
            IPlatformApplication.Current = this;

            DispatcherProvider.SetCurrent(new TvOSDispatcherProvider());

            _application = _mauiApp.Services.GetRequiredService<IApplication>();

            // Create the application handler
            var appHandler = _mauiContext.Handlers.GetHandler(_application.GetType());
            if (appHandler != null)
            {
                appHandler.SetMauiContext(_mauiContext);
                appHandler.SetVirtualView(_application);
            }

            // Create the window through MAUI's pipeline
            var activationState = new ActivationState(_mauiContext);
            var window = _application.CreateWindow(activationState);

            // Create window handler
            var windowHandler = _mauiContext.Handlers.GetHandler(window.GetType());
            if (windowHandler != null)
            {
                windowHandler.SetMauiContext(_mauiContext);
                windowHandler.SetVirtualView(window);
            }

            // Store the platform window
            if (window.Handler?.PlatformView is UIWindow uiWindow)
            {
                Window = uiWindow;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MAUI STARTUP EXCEPTION: {ex}");
            throw;
        }
    }
}
