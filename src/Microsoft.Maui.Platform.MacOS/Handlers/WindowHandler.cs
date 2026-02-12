using CoreGraphics;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Flipped NSView used as NSWindow.ContentView so MAUI's top-left coordinate system works.
/// </summary>
internal class FlippedNSView : NSView
{
    public FlippedNSView()
    {
        WantsLayer = true;
    }

    public override bool IsFlipped => true;
}

public partial class WindowHandler : ElementHandler<IWindow, NSWindow>
{
    public static readonly IPropertyMapper<IWindow, WindowHandler> Mapper =
        new PropertyMapper<IWindow, WindowHandler>(ElementMapper)
        {
            [nameof(IWindow.Title)] = MapTitle,
            [nameof(IWindow.Content)] = MapContent,
        };

    FlippedNSView? _contentContainer;

    public WindowHandler() : base(Mapper)
    {
    }

    protected override NSWindow CreatePlatformElement()
    {
        var style = NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable;
        var window = new NSWindow(
            new CGRect(0, 0, 1280, 720),
            style,
            NSBackingStore.Buffered,
            false);

        window.Center();

        // Use a flipped NSView as ContentView so subviews use top-left origin
        _contentContainer = new FlippedNSView();
        window.ContentView = _contentContainer;

        window.MakeKeyAndOrderFront(null);

        return window;
    }

    public static void MapTitle(WindowHandler handler, IWindow window)
    {
        if (handler.PlatformView != null)
            handler.PlatformView.Title = window.Title ?? string.Empty;
    }

    public static void MapContent(WindowHandler handler, IWindow window)
    {
        if (handler.MauiContext == null || window.Content == null)
            return;

        var page = window.Content;
        var pageHandler = page.ToHandler(handler.MauiContext);
        var pageView = pageHandler.ToPlatformView();

        if (handler._contentContainer != null)
        {
            foreach (var subview in handler._contentContainer.Subviews)
                subview.RemoveFromSuperview();

            pageView.Frame = handler._contentContainer.Bounds;
            pageView.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
            handler._contentContainer.AddSubview(pageView);
        }
    }
}
