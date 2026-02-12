using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Maui.Platform.MacOS.Hosting;

public class MacOSMauiContext : IMauiContext
{
    readonly IServiceProvider _services;
    IMauiHandlersFactory? _handlers;

    public MacOSMauiContext(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    MacOSMauiContext(IServiceProvider services, IMauiHandlersFactory handlers)
    {
        _services = services;
        _handlers = handlers;
    }

    public IServiceProvider Services => _services;

    public IMauiHandlersFactory Handlers =>
        _handlers ??= _services.GetRequiredService<IMauiHandlersFactory>();

    public MacOSMauiContext MakeWindowScope(IServiceProvider windowServices)
    {
        return new MacOSMauiContext(windowServices, Handlers);
    }
}
