using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Sample;

public class App : Microsoft.Maui.Controls.Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(new NavigationPage(new MainPage { Title = "Controls" }) { Title = "Controls" });
        tabbedPage.Children.Add(new NavigationPage(new EssentialsPage { Title = "Essentials" }) { Title = "Essentials" });
        tabbedPage.Children.Add(new NavigationPage(new StoragePage { Title = "Storage" }) { Title = "Storage" });
        return new Window(tabbedPage);
    }
}
