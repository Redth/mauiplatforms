using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Sample.Pages;

public class TableViewPage : ContentPage
{
	public TableViewPage()
	{
		Title = "TableView";

		var tableView = new TableView
		{
			HasUnevenRows = true,
			Intent = TableIntent.Settings,
			Root = new TableRoot("Settings")
			{
				new TableSection("Account")
				{
					new TextCell { Text = "Username", Detail = "john.appleseed" },
					new TextCell { Text = "Email", Detail = "john@example.com" },
					new ImageCell { Text = "Profile Photo", Detail = "Tap to change" },
				},
				new TableSection("Preferences")
				{
					new SwitchCell { Text = "Dark Mode", On = false },
					new SwitchCell { Text = "Notifications", On = true },
					new SwitchCell { Text = "Auto-update", On = true },
				},
				new TableSection("Input")
				{
					new EntryCell { Label = "Name", Text = "John", Placeholder = "Enter your name" },
					new EntryCell { Label = "Bio", Placeholder = "Write something..." },
				},
				new TableSection("About")
				{
					new TextCell { Text = "Version", Detail = "1.0.0" },
					new TextCell { Text = "Build", Detail = "2024.02.20" },
					new TextCell { Text = "License", Detail = "MIT" },
				},
			}
		};

		Content = new VerticalStackLayout
		{
			Spacing = 10,
			Padding = new Thickness(24),
			Children =
			{
				new Label { Text = "TableView", FontSize = 24, FontAttributes = FontAttributes.Bold },
				new BoxView { HeightRequest = 2, Color = Colors.DodgerBlue },
				new Label { Text = "Settings-style grouped table with various cell types:", FontSize = 14, TextColor = Colors.Gray },
				new Border
				{
					Stroke = Colors.LightGray,
					StrokeThickness = 1,
					Content = tableView,
					HeightRequest = 500,
				},
			}
		};
	}
}
