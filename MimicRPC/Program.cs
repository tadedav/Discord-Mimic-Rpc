using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

class Program
{
	[STAThread]
	static int Main(string[] args)
	{
		using Mutex mutex = new(true, "MimicRPC", out bool first);
		if (!first) return 0;
		return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().StartWithClassicDesktopLifetime(args);
	}
}

class App : Application
{
	public override void Initialize()
	{
		Name = "MimicRPC";
		Styles.Add(new FluentTheme());
		RequestedThemeVariant = ThemeVariant.Dark;

		Styles.Add(new Style(s => s.OfType<Button>())
			{ Setters = { new Setter(Button.CursorProperty, new Cursor(StandardCursorType.Hand)) } });

		BtnColors("green", "#248046");
		BtnColors("red", "#ED4245");
		BtnColors("stop", "#A32B2E");
		BtnColors("accent", "#5865F2");
		BtnColors("orange", "#F0890C");

		Styles.Add(new Style(s => s.OfType<Button>().Class("ghost-red"))
			{ Setters = {
				new Setter(Button.BackgroundProperty, Brushes.Transparent),
				new Setter(Button.BorderBrushProperty, Brush("#BBED4245")),
				new Setter(Button.ForegroundProperty, Brush("#BBED4245")) } });

		Styles.Add(new Style(s => s.OfType<Button>().Class("recent-item-active"))
			{ Setters = {
				new Setter(Button.BackgroundProperty, Brush("#1A248046")),
				new Setter(Button.ForegroundProperty, Brush("#3BA55D")) } });

		Styles.Add(new Style(s => s.OfType<Button>().Class("recent-item"))
			{ Setters = {
				new Setter(Button.BackgroundProperty, Brush("#1E2330")),
				new Setter(Button.ForegroundProperty, Brush("#DCDDDE")) } });
		Styles.Add(new Style(s => s.OfType<Button>().Class("recent-item").Class(":pointerover"))
			{ Setters = { new Setter(Button.BackgroundProperty, Brush("#252C3D")) } });
		Styles.Add(new Style(s => s.OfType<Button>().Class("recent-item").Class(":pressed"))
			{ Setters = { new Setter(Button.BackgroundProperty, Brush("#1A2235")) } });

		Styles.Add(new Style(s => s.OfType<TabItem>())
			{ Setters = { new Setter(TabItem.PaddingProperty, new Thickness(10, 4)) } });

		Styles.Add(new Style(s => s.OfType<TextBox>())
			{ Setters = { new Setter(TextBox.BackgroundProperty, Brush("#1E2330")) } });
		Styles.Add(new Style(s => s.OfType<AutoCompleteBox>())
			{ Setters = { new Setter(AutoCompleteBox.BackgroundProperty, Brush("#1E2330")) } });
	}

	void BtnColors(string cls, string bg)
	{
		Styles.Add(new Style(s => s.OfType<Button>().Class(cls))
			{ Setters = { new Setter(Button.BackgroundProperty, Brush(bg)),
				new Setter(Button.ForegroundProperty, Brushes.White) } });
	}

	static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
			d.MainWindow = new MainWindow();
		base.OnFrameworkInitializationCompleted();
	}
}