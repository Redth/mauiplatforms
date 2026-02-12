using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.MacOS.Controls;

namespace Sample;

public class BlazorPage : ContentPage
{
    public BlazorPage()
    {
        Title = "Blazor WebView";
        BackgroundColor = Color.FromArgb("#1A1A2E");

        var backButton = new Button
        {
            Text = "← Back",
            BackgroundColor = Color.FromArgb("#FF6B6B"),
            TextColor = Colors.White,
            HeightRequest = 44,
            Margin = new Thickness(10, 10, 10, 0)
        };
        backButton.Clicked += async (s, e) => await Navigation.PopAsync();

        var blazorWebView = new MacOSBlazorWebView
        {
            HostPage = "wwwroot/index.html",
            BackgroundColor = Color.FromArgb("#1A1A2E"),
            HeightRequest = 400,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        };

        blazorWebView.RootComponents.Add(new BlazorRootComponent
        {
            Selector = "#app",
#if !TVOS
            ComponentType = typeof(SampleMac.Components.Counter)
#endif
        });

        Content = new VerticalStackLayout
        {
            Children = { backButton, blazorWebView }
        };
    }
}
