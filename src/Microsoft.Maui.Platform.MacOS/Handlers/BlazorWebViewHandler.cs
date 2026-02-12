using System.Globalization;
using CoreGraphics;
using Foundation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.MacOS.Controls;
using WebKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public partial class BlazorWebViewHandler : MacOSViewHandler<MacOSBlazorWebView, WKWebView>
{
    public static readonly IPropertyMapper<MacOSBlazorWebView, BlazorWebViewHandler> Mapper =
        new PropertyMapper<MacOSBlazorWebView, BlazorWebViewHandler>(ViewMapper);

    internal static string AppOrigin { get; } = "app://0.0.0.1/";
    internal static Uri AppOriginUri { get; } = new(AppOrigin);

    private const string BlazorInitScript = @"
        window.__receiveMessageCallbacks = [];
        window.__dispatchMessageCallback = function(message) {
            window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
        };
        window.external = {
            sendMessage: function(message) {
                window.webkit.messageHandlers.webwindowinterop.postMessage(message);
            },
            receiveMessage: function(callback) {
                window.__receiveMessageCallbacks.push(callback);
            }
        };

        Blazor.start();

        (function () {
            window.onpageshow = function(event) {
                if (event.persisted) {
                    window.location.reload();
                }
            };
        })();
    ";

    MacOSWebViewManager? _webviewManager;

    string? HostPage => VirtualView?.HostPage;
    new IServiceProvider? Services => MauiContext?.Services;

    public BlazorWebViewHandler() : base(Mapper)
    {
    }

    protected override WKWebView CreatePlatformView()
    {
        Log("[CreatePV] called");
        var config = new WKWebViewConfiguration();

        config.UserContentController.AddScriptMessageHandler(
            new WebViewScriptMessageHandler(MessageReceived), "webwindowinterop");
        config.UserContentController.AddUserScript(new WKUserScript(
            new NSString(BlazorInitScript), WKUserScriptInjectionTime.AtDocumentEnd, true));

        config.SetUrlSchemeHandler(new SchemeHandler(this), urlScheme: "app");

        var webview = new WKWebView(CGRect.Empty, config);
        config.Preferences.SetValueForKey(NSObject.FromObject(true), new NSString("developerExtrasEnabled"));

        return webview;
    }

    protected override void ConnectHandler(WKWebView platformView)
    {
        Log("[Connect] called");
        base.ConnectHandler(platformView);
        StartWebViewCoreIfPossible();
        Log("[Connect] done");
    }

    protected override void DisconnectHandler(WKWebView platformView)
    {
        platformView.StopLoading();

        if (_webviewManager != null)
        {
            _webviewManager.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _webviewManager = null;
        }

        base.DisconnectHandler(platformView);
    }

    void MessageReceived(Uri uri, string message)
    {
        _webviewManager?.MessageReceivedInternal(uri, message);
    }

    static void Log(string msg)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "blazor_debug.log");
        File.AppendAllText(logPath, msg + "\n");
    }

    void StartWebViewCoreIfPossible()
    {
        Log("[Start] called");
        if (HostPage == null || Services == null || _webviewManager != null)
        {
            Log($"[Start] skip: HostPage={HostPage}, Services={Services != null}, mgr={_webviewManager != null}");
            return;
        }

        try
        {
            var contentRootDir = Path.GetDirectoryName(HostPage!) ?? string.Empty;
            var hostPageRelativePath = Path.GetRelativePath(contentRootDir, HostPage!);
            Log($"[Start] contentRoot='{contentRootDir}', hostPage='{hostPageRelativePath}'");

            var fileProvider = new MacOSMauiAssetFileProvider(contentRootDir);
            var testFile = fileProvider.GetFileInfo(hostPageRelativePath);
            Log($"[Start] hostPage exists={testFile?.Exists}, path={testFile?.PhysicalPath}");

            var dispatcher = Services!.GetService<IDispatcher>()
                ?? new Microsoft.Maui.Platform.MacOS.Dispatching.MacOSDispatcher();
            Log($"[Start] dispatcher={dispatcher.GetType().FullName}");

            var jsComponents = new Microsoft.AspNetCore.Components.Web.JSComponentConfigurationStore();

            _webviewManager = new MacOSWebViewManager(
                PlatformView,
                Services!,
                new MacOSBlazorDispatcher(dispatcher),
                fileProvider,
                jsComponents,
                contentRootDir,
                hostPageRelativePath);

            foreach (var rootComponent in VirtualView.RootComponents)
            {
                if (rootComponent.ComponentType != null && rootComponent.Selector != null)
                {
                    Log($"[Start] AddRoot: {rootComponent.Selector} -> {rootComponent.ComponentType.FullName}");
                    var parameters = rootComponent.Parameters != null
                        ? ParameterView.FromDictionary(rootComponent.Parameters)
                        : ParameterView.Empty;
                    _ = _webviewManager.AddRootComponentAsync(rootComponent.ComponentType, rootComponent.Selector, parameters);
                }
            }

            Log("[Start] Navigate");
            _webviewManager.Navigate(VirtualView.StartPath);
            Log("[Start] done");
        }
        catch (Exception ex)
        {
            Log($"[Start] ERROR: {ex}");
        }
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var width = double.IsPositiveInfinity(widthConstraint) ? 400 : widthConstraint;
        var height = double.IsPositiveInfinity(heightConstraint) ? 400 : heightConstraint;
        return new Size(width, height);
    }

    sealed class WebViewScriptMessageHandler : NSObject, IWKScriptMessageHandler
    {
        readonly Action<Uri, string> _messageReceivedAction;

        public WebViewScriptMessageHandler(Action<Uri, string> messageReceivedAction)
        {
            _messageReceivedAction = messageReceivedAction;
        }

        public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            _messageReceivedAction(AppOriginUri, ((NSString)message.Body).ToString());
        }
    }

    sealed class SchemeHandler : NSObject, IWKUrlSchemeHandler
    {
        readonly BlazorWebViewHandler _handler;

        public SchemeHandler(BlazorWebViewHandler handler) => _handler = handler;

        [Export("webView:startURLSchemeTask:")]
        public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
            var url = urlSchemeTask.Request.Url?.AbsoluteString;
            Log($"[Scheme] req: {url}");
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                var responseBytes = GetResponseBytes(url, out var contentType, out var statusCode);
                Log($"[Scheme] resp: status={statusCode}, type={contentType}, len={responseBytes.Length}");
                using var dic = new NSMutableDictionary<NSString, NSString>();

                if (statusCode == 200)
                {
                    dic.Add((NSString)"Content-Length", (NSString)responseBytes.Length.ToString(CultureInfo.InvariantCulture));
                    dic.Add((NSString)"Content-Type", (NSString)contentType);
                    dic.Add((NSString)"Cache-Control", (NSString)"no-cache, max-age=0, must-revalidate, no-store");
                }
                else
                {
                    dic.Add((NSString)"Content-Length", (NSString)"0");
                    dic.Add((NSString)"Content-Type", (NSString)"text/plain");
                }

                if (urlSchemeTask.Request.Url != null)
                {
                    using var response = new NSHttpUrlResponse(urlSchemeTask.Request.Url, statusCode, "HTTP/1.1", dic);
                    urlSchemeTask.DidReceiveResponse(response);
                }

                urlSchemeTask.DidReceiveData(NSData.FromArray(statusCode == 200 ? responseBytes : Array.Empty<byte>()));
                urlSchemeTask.DidFinish();
            }
            catch
            {
                // Swallow errors to avoid crashing the WKWebView process
            }
        }

        byte[] GetResponseBytes(string? url, out string contentType, out int statusCode)
        {
            var uri = new Uri(url!);
            var allowFallbackOnHostPage = AppOriginUri.IsBaseOf(uri)
                && !uri.AbsolutePath.StartsWith("/_framework/", StringComparison.Ordinal)
                && !uri.AbsolutePath.StartsWith("/_content/", StringComparison.Ordinal);
            var queryIndex = url?.IndexOf('?') ?? -1;
            if (queryIndex >= 0)
                url = url![..queryIndex];

            if (_handler._webviewManager!.TryGetResponseContentInternal(url!, allowFallbackOnHostPage, out statusCode, out var statusMsg, out var content, out var headers))
            {
                statusCode = 200;
                using var ms = new MemoryStream();
                content.CopyTo(ms);
                content.Dispose();
                contentType = headers["Content-Type"];
                return ms.ToArray();
            }

            statusCode = 404;
            contentType = string.Empty;
            return Array.Empty<byte>();
        }

        [Export("webView:stopURLSchemeTask:")]
        public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
        }
    }
}
