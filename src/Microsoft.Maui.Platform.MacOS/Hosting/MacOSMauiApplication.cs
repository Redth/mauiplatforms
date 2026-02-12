using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.MacOS.Dispatching;
using Microsoft.Maui.Platform.MacOS.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Hosting;

[Register("MacOSMauiApplication")]
public abstract class MacOSMauiApplication : NSApplicationDelegate, IPlatformApplication
{
    MauiApp? _mauiApp;
    MacOSMauiContext? _mauiContext;
    IApplication? _application;

    public IServiceProvider Services => _mauiApp?.Services ?? throw new InvalidOperationException("MauiApp not initialized");

    IApplication IPlatformApplication.Application => _application ?? throw new InvalidOperationException("Application not initialized");

    public MacOSMauiContext MauiContext => _mauiContext ?? throw new InvalidOperationException("MauiContext not initialized");

    protected abstract MauiApp CreateMauiApp();

    public override void DidFinishLaunching(NSNotification notification)
    {
        try
        {
            _mauiApp = CreateMauiApp();
            _mauiContext = new MacOSMauiContext(_mauiApp.Services);
            IPlatformApplication.Current = this;

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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MAUI STARTUP EXCEPTION: {ex}");
            throw;
        }
    }
}
