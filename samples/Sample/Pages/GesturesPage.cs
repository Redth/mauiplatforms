using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Sample.Pages;

public class GesturesPage : ContentPage
{
	public GesturesPage()
	{
		Title = "Gestures";

		// Tap gesture demo
		int tapCount = 0;
		var tapBox = new BoxView
		{
			Color = Colors.DodgerBlue,
			WidthRequest = 120,
			HeightRequest = 80,
			HorizontalOptions = LayoutOptions.Start,
		};
		var tapLabel = new Label { Text = "Tap the box! Taps: 0", FontSize = 14 };
		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += (s, e) =>
		{
			tapCount++;
			tapLabel.Text = $"Tap the box! Taps: {tapCount}";
			tapBox.Color = tapCount % 2 == 0 ? Colors.DodgerBlue : Colors.Coral;
		};
		tapBox.GestureRecognizers.Add(tapGesture);

		// Pan gesture demo
		double panTotalX = 0, panTotalY = 0;
		var panBox = new BoxView
		{
			Color = Colors.MediumSeaGreen,
			WidthRequest = 80,
			HeightRequest = 80,
			HorizontalOptions = LayoutOptions.Start,
		};
		// Container so pan gesture area doesn't move with the visual transform
		var panContainer = new Grid
		{
			WidthRequest = 400,
			HeightRequest = 120,
			BackgroundColor = Color.FromArgb("#F0F0F0"),
			HorizontalOptions = LayoutOptions.Start,
			Children = { panBox },
		};
		var panGesture = new PanGestureRecognizer();
		panGesture.PanUpdated += (s, e) =>
		{
			switch (e.StatusType)
			{
				case GestureStatus.Running:
					panBox.TranslationX = panTotalX + e.TotalX;
					panBox.TranslationY = panTotalY + e.TotalY;
					break;
				case GestureStatus.Completed:
					panTotalX = panBox.TranslationX;
					panTotalY = panBox.TranslationY;
					break;
			}
		};
		panContainer.GestureRecognizers.Add(panGesture);

		// Swipe gesture demo
		var swipeLabel = new Label
		{
			Text = "Click and drag quickly, then release",
			FontSize = 14,
		};
		var swipeResultLabel = new Label
		{
			Text = "Swipe result: (none)",
			FontSize = 16,
			FontAttributes = FontAttributes.Bold,
			TextColor = Colors.DodgerBlue,
		};
		var swipeBox = new BoxView
		{
			Color = Color.FromArgb("#E8F0FE"),
			WidthRequest = 300,
			HeightRequest = 80,
			HorizontalOptions = LayoutOptions.Start,
		};
		foreach (var dir in new[] { SwipeDirection.Left, SwipeDirection.Right, SwipeDirection.Up, SwipeDirection.Down })
		{
			var swipeGesture = new SwipeGestureRecognizer { Direction = dir };
			swipeGesture.Swiped += (s, e) => swipeResultLabel.Text = $"Swiped: {e.Direction}";
			swipeBox.GestureRecognizers.Add(swipeGesture);
		}

		// Pinch gesture demo
		var pinchBox = new BoxView
		{
			Color = Colors.MediumPurple,
			WidthRequest = 100,
			HeightRequest = 100,
			HorizontalOptions = LayoutOptions.Start,
		};
		var pinchLabel = new Label { Text = "Scale: 1.00", FontSize = 14 };
		double currentScale = 1;
		var pinchGesture = new PinchGestureRecognizer();
		pinchGesture.PinchUpdated += (s, e) =>
		{
			switch (e.Status)
			{
				case GestureStatus.Running:
					pinchBox.Scale = currentScale * e.Scale;
					pinchLabel.Text = $"Scale: {pinchBox.Scale:F2}";
					break;
				case GestureStatus.Completed:
					currentScale = pinchBox.Scale;
					break;
			}
		};
		pinchBox.GestureRecognizers.Add(pinchGesture);

		// Pointer gesture demo
		var pointerBox = new BoxView
		{
			Color = Colors.SteelBlue,
			WidthRequest = 150,
			HeightRequest = 80,
			HorizontalOptions = LayoutOptions.Start,
		};
		var pointerLabel = new Label { Text = "Hover over the box", FontSize = 14 };
		var pointerGesture = new PointerGestureRecognizer();
		pointerGesture.PointerEntered += (s, e) =>
		{
			pointerLabel.Text = "Pointer: Entered";
			pointerBox.Color = Colors.Orange;
		};
		pointerGesture.PointerExited += (s, e) =>
		{
			pointerLabel.Text = "Pointer: Exited";
			pointerBox.Color = Colors.SteelBlue;
		};
		pointerGesture.PointerMoved += (s, e) =>
		{
			var pos = e.GetPosition(pointerBox);
			pointerLabel.Text = $"Pointer: Moved ({pos?.X:F0}, {pos?.Y:F0})";
		};
		pointerBox.GestureRecognizers.Add(pointerGesture);

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Spacing = 16,
				Padding = new Thickness(24),
				Children =
				{
					new Label { Text = "Gesture Recognizers", FontSize = 24, FontAttributes = FontAttributes.Bold },
					new BoxView { HeightRequest = 2, Color = Colors.DodgerBlue },

					SectionHeader("TapGestureRecognizer"),
					tapLabel,
					tapBox,

					Separator(),

					SectionHeader("PanGestureRecognizer"),
					new Label { Text = "Click and drag in the gray area:", FontSize = 14 },
					panContainer,

					Separator(),

					SectionHeader("SwipeGestureRecognizer"),
					swipeLabel,
					swipeBox,
					swipeResultLabel,

					Separator(),

					SectionHeader("PinchGestureRecognizer"),
					pinchLabel,
					pinchBox,

					Separator(),

					SectionHeader("PointerGestureRecognizer"),
					pointerLabel,
					pointerBox,
				}
			}
		};
	}

	static Label SectionHeader(string text) => new()
	{
		Text = text,
		FontSize = 16,
		FontAttributes = FontAttributes.Bold,
		TextColor = Colors.DarkSlateGray,
	};

	static BoxView Separator() => new() { HeightRequest = 1, Color = Colors.LightGray };
}
