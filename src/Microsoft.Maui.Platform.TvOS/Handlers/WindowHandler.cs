using CoreGraphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public partial class WindowHandler : ElementHandler<IWindow, UIWindow>
{
    public static readonly IPropertyMapper<IWindow, WindowHandler> Mapper =
        new PropertyMapper<IWindow, WindowHandler>(ElementMapper)
        {
            [nameof(IWindow.Title)] = MapTitle,
            [nameof(IWindow.Content)] = MapContent,
        };

    UIViewController? _rootViewController;

    public WindowHandler() : base(Mapper)
    {
    }

#pragma warning disable CA1422
    protected override UIWindow CreatePlatformElement()
    {
        var window = new UIWindow(UIScreen.MainScreen.Bounds);
        _rootViewController = new UIViewController();
        window.RootViewController = _rootViewController;
        window.MakeKeyAndVisible();
        return window;
    }
#pragma warning restore CA1422

    public static void MapTitle(WindowHandler handler, IWindow window)
    {
        // tvOS windows don't have titles — no-op
    }

    public static void MapContent(WindowHandler handler, IWindow window)
    {
        if (handler.MauiContext == null || window.Content == null)
            return;

        var page = window.Content;
        var pageHandler = page.ToHandler(handler.MauiContext);
        var pageView = pageHandler.ToPlatformView();

        if (handler._rootViewController != null)
        {
            foreach (var subview in handler._rootViewController.View!.Subviews)
                subview.RemoveFromSuperview();

            pageView.Frame = handler._rootViewController.View.Bounds;
            pageView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            handler._rootViewController.View.AddSubview(pageView);
        }
    }
}
