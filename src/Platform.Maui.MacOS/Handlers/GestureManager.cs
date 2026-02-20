using AppKit;
using CoreGraphics;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Manages gesture recognizer attachment for macOS NSViews.
/// Called by MacOSViewHandler when GestureRecognizers are added/removed.
/// </summary>
public static class GestureManager
{
    public static void SetupGestures(NSView platformView, IView virtualView)
    {
        if (virtualView is not View mauiView)
            return;

        // Clear existing managed gesture recognizers
        ClearManagedGestures(platformView);

        foreach (var gestureRecognizer in mauiView.GestureRecognizers)
        {
            switch (gestureRecognizer)
            {
                case TapGestureRecognizer tap:
                    AddTapGesture(platformView, tap);
                    break;
                case PanGestureRecognizer pan:
                    AddPanGesture(platformView, pan);
                    break;
                case PointerGestureRecognizer pointer:
                    AddPointerGesture(platformView, pointer);
                    break;
            }
        }
    }

    static void ClearManagedGestures(NSView view)
    {
        if (view is MacOSContainerView container)
            container.InterceptChildHitTesting = false;

        if (view.GestureRecognizers == null)
            return;

        var toRemove = new List<NSGestureRecognizer>();
        foreach (var gr in view.GestureRecognizers)
        {
            if (gr is MacOSTapGestureRecognizer or MacOSPanGestureRecognizer)
                toRemove.Add(gr);
        }
        foreach (var gr in toRemove)
            view.RemoveGestureRecognizer(gr);

        // Remove tracking areas for pointer gestures
        foreach (var area in view.TrackingAreas())
        {
            if (area is MacOSPointerTrackingArea)
                view.RemoveTrackingArea(area);
        }
    }

    static void AddTapGesture(NSView view, TapGestureRecognizer tap)
    {
        var recognizer = new MacOSTapGestureRecognizer(tap)
        {
            NumberOfClicksRequired = (nint)tap.NumberOfTapsRequired,
        };
        view.AddGestureRecognizer(recognizer);

        // AppKit delivers events only to the hit-test view's own gesture recognizers,
        // not ancestors. Intercept child hit testing so clicks on child views
        // (e.g. Label NSTextFields) still fire this view's gesture recognizer.
        if (view is MacOSContainerView container)
            container.InterceptChildHitTesting = true;
    }

    static void AddPanGesture(NSView view, PanGestureRecognizer pan)
    {
        var recognizer = new MacOSPanGestureRecognizer(pan);
        view.AddGestureRecognizer(recognizer);
    }

    static void AddPointerGesture(NSView view, PointerGestureRecognizer pointer)
    {
        var area = new MacOSPointerTrackingArea(view, pointer);
        view.AddTrackingArea(area);
    }
}

internal class MacOSTapGestureRecognizer : NSClickGestureRecognizer
{
    readonly TapGestureRecognizer _tapGesture;

    public MacOSTapGestureRecognizer(TapGestureRecognizer tapGesture)
    {
        _tapGesture = tapGesture;
        Action = new ObjCRuntime.Selector("handleTap:");
        Target = this;
    }

    [Foundation.Export("handleTap:")]
    void HandleTap(NSGestureRecognizer recognizer)
    {
        _tapGesture.Command?.Execute(_tapGesture.CommandParameter);

        // TapGestureRecognizer.SendTapped is internal in MAUI — invoke via reflection
        var sendTapped = typeof(TapGestureRecognizer).GetMethod(
            "SendTapped", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (sendTapped != null)
        {
            var parent = (_tapGesture as IElement)?.FindParentOfType<View>();
            var parameters = sendTapped.GetParameters();
            if (parameters.Length == 1)
                sendTapped.Invoke(_tapGesture, new object?[] { parent });
            else if (parameters.Length == 2)
                sendTapped.Invoke(_tapGesture, new object?[] { parent, null });
        }
    }
}

internal class MacOSPanGestureRecognizer : NSPanGestureRecognizer
{
    readonly PanGestureRecognizer _panGesture;
    CGPoint _startPoint;

    public MacOSPanGestureRecognizer(PanGestureRecognizer panGesture)
    {
        _panGesture = panGesture;
        Action = new ObjCRuntime.Selector("handlePan:");
        Target = this;
    }

    [Foundation.Export("handlePan:")]
    void HandlePan(NSPanGestureRecognizer recognizer)
    {
        var translation = recognizer.TranslationInView(recognizer.View);

        switch (recognizer.State)
        {
            case NSGestureRecognizerState.Began:
                _startPoint = translation;
                ((IPanGestureController)_panGesture).SendPanStarted(
                    recognizer.View?.FindMauiView(), PanGestureRecognizer.CurrentId.Value);
                break;
            case NSGestureRecognizerState.Changed:
                ((IPanGestureController)_panGesture).SendPan(
                    recognizer.View?.FindMauiView(),
                    translation.X - _startPoint.X,
                    translation.Y - _startPoint.Y,
                    PanGestureRecognizer.CurrentId.Value);
                break;
            case NSGestureRecognizerState.Ended:
            case NSGestureRecognizerState.Cancelled:
                ((IPanGestureController)_panGesture).SendPanCompleted(
                    recognizer.View?.FindMauiView(), PanGestureRecognizer.CurrentId.Value);
                break;
        }
    }
}

internal class MacOSPointerTrackingArea : NSTrackingArea
{
    readonly PointerGestureRecognizer _pointerGesture;

    public MacOSPointerTrackingArea(NSView view, PointerGestureRecognizer pointerGesture)
        : base(view.Bounds,
            NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.MouseMoved |
            NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect,
            view, null)
    {
        _pointerGesture = pointerGesture;
    }

    public PointerGestureRecognizer Recognizer => _pointerGesture;
}

internal static class GestureExtensions
{
    public static View? FindMauiView(this NSView? view)
    {
        // Walk up the view hierarchy to find the MAUI view element
        return null; // Placeholder — requires handler → VirtualView lookup
    }

    public static T? FindParentOfType<T>(this IElement element) where T : class
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current is T result)
                return result;
            current = current.Parent;
        }
        return null;
    }

    public static void SendTapped(this IGestureRecognizer recognizer, View? view)
    {
        if (recognizer is TapGestureRecognizer tap)
            tap.Command?.Execute(tap.CommandParameter);
    }
}
