using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

class MainWindow : Window
{
	readonly Rpc _rpc = new();
	readonly DateTime _appLaunch = DateTime.UtcNow;
	DateTime? _rpcSince;
	(string Name, string Id) _current;
	readonly AutoCompleteBox _game;
	readonly Button _xBtn;
	readonly Button _btnUpdate;
	readonly RadioButton _rbNow;
	readonly RadioButton _rbLaunch;
	readonly RadioButton _fxRbStart;
	readonly RadioButton _fxRbOpen;
	readonly TextBox _fxAppId;
	readonly TextBox _fxDetails;
	readonly TextBox _fxState;
	readonly TextBox _fxLargeKey;
	readonly TextBox _fxLargeText;
	readonly TextBox _fxSmallKey;
	readonly TextBox _fxSmallText;
	readonly TextBox _fxPartySize;
	readonly TextBox _fxPartyMax;
	readonly TextBox _fxBtn1Label;
	readonly TextBox _fxBtn1Url;
	readonly TextBox _fxBtn2Label;
	readonly TextBox _fxBtn2Url;
	readonly TabControl _tabs;
	readonly Button _btnStart;
	readonly Button _btnStop;
	readonly TextBlock _statusGame;
	readonly TextBlock _statusTime;
	readonly DispatcherTimer _ticker;
	readonly Settings _settings = Settings.Load();
	readonly StackPanel _recentPanel = new() { Spacing = 6 };
	readonly Button _btnClear = new() { Content = "Clear history", Padding = new Thickness(12, 6), HorizontalAlignment = HorizontalAlignment.Left };

	const string AppVersion = "Beta 0.3";
	const string DbUpdated = "May 28, 2026";
	const int DbCount = 22_843;

	static readonly SolidColorBrush GreenBrush = new(Color.Parse("#3BA55D"));
	static readonly SolidColorBrush MutedBrush = new(Color.Parse("#949BA4"));

	public MainWindow()
	{
		Title = "MimicRPC";
		Width = 380;
		SizeToContent = SizeToContent.Height;
		CanResize = false;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;
		Background = new SolidColorBrush(Color.Parse("#11141C"));

		_game = new AutoCompleteBox
		{
			Watermark = "Search game…",
			FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
			MinimumPrefixLength = 1,
			ItemsSource = GameDb.Names
		};

		_xBtn = new Button { Content = "✕", IsVisible = false, Padding = new Thickness(8, 4) };
		_xBtn.Classes.Add("red");
		_xBtn.Click += (_, _) => _game.Text = "";

		_rbNow = Radio("Since Start", true);
		_rbLaunch = Radio("Since Open");
		_fxRbStart = Radio("Since Start", true, "cx");
		_fxRbOpen = Radio("Since Open", false, "cx");

		_btnUpdate = new Button { Content = "Update", IsVisible = false, Padding = new Thickness(12, 6) };
		_btnUpdate.Classes.Add("orange");
		_btnUpdate.Click += OnUpdate;

		StackPanel gameContent = Stack(14, 9,
			MakeGameRow(),
			Row(12, _rbNow, _rbLaunch),
			_btnUpdate
		);

		_fxAppId = Field("App ID (required)");
		_fxDetails = Field("Details — line 1 under game name");
		_fxState = Field("State — line 2 under details");
		_fxLargeKey = Field("Image key");
		_fxLargeText = Field("Tooltip");
		_fxSmallKey = Field("Image key");
		_fxSmallText = Field("Tooltip");
		_fxPartySize = Field("Current");
		_fxPartyMax = Field("Max");
		_fxBtn1Label = Field("Label");
		_fxBtn1Url = Field("URL");
		_fxBtn2Label = Field("Label");
		_fxBtn2Url = Field("URL");

		if (_settings.Custom is CustomAppSettings csa)
		{
			_fxAppId.Text = csa.AppId;
			_fxDetails.Text = csa.Details;
			_fxState.Text = csa.State;
			_fxLargeKey.Text = csa.LargeKey;
			_fxLargeText.Text = csa.LargeText;
			_fxSmallKey.Text = csa.SmallKey;
			_fxSmallText.Text = csa.SmallText;
			_fxPartySize.Text = csa.PartySize;
			_fxPartyMax.Text = csa.PartyMax;
			_fxBtn1Label.Text = csa.Btn1Label;
			_fxBtn1Url.Text = csa.Btn1Url;
			_fxBtn2Label.Text = csa.Btn2Label;
			_fxBtn2Url.Text = csa.Btn2Url;
		}

		ScrollViewer customContent = new()
		{
			MaxHeight = 300,
			Content = Stack(14, 9,
				Group("App ID", _fxAppId),
				Group("Timestamp", Row(12, _fxRbStart, _fxRbOpen)),
				Group("Details", _fxDetails),
				Group("State", _fxState),
				Group("Large Image", Split(_fxLargeKey, _fxLargeText)),
				Group("Small Image", Split(_fxSmallKey, _fxSmallText)),
				Group("Party Size", Split(_fxPartySize, _fxPartyMax)),
				Group("Button 1", Split(_fxBtn1Label, _fxBtn1Url)),
				Group("Button 2", Split(_fxBtn2Label, _fxBtn2Url))
			)
		};

		_btnClear.Classes.Add("ghost-red");
		_btnClear.Click += (_, _) => { _settings.ClearAll(); RefreshRecentPanel(); };

		RefreshRecentPanel();

		ScrollViewer welcomeContent = new()
		{
			MaxHeight = 280,
			Content = Stack(14, 12,
				new TextBlock { Text = "Welcome to MimicRPC", FontSize = 13, FontWeight = FontWeight.SemiBold },
				new TextBlock { Text = "Recent", FontSize = 12, Opacity = 0.55 },
				_recentPanel
			)
		};

		Button btnClearCustom = new() { Content = "Clear custom app", Padding = new Thickness(12, 6), HorizontalAlignment = HorizontalAlignment.Left };
		btnClearCustom.Classes.Add("ghost-red");
		btnClearCustom.Click += (_, _) =>
		{
			_settings.ClearCustom();
			_fxAppId.Text = _fxDetails.Text = _fxState.Text = null;
			_fxLargeKey.Text = _fxLargeText.Text = _fxSmallKey.Text = _fxSmallText.Text = null;
			_fxPartySize.Text = _fxPartyMax.Text = null;
			_fxBtn1Label.Text = _fxBtn1Url.Text = _fxBtn2Label.Text = _fxBtn2Url.Text = null;
		};

		StackPanel settingsContent = Stack(14, 10, _btnClear, btnClearCustom);

		_tabs = new TabControl();
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Welcome", FontSize = 13 }, Content = welcomeContent });
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Game", FontSize = 13 }, Content = gameContent });
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Custom", FontSize = 13 }, Content = customContent });
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Settings", FontSize = 13 }, Content = settingsContent });

		_statusGame = new TextBlock { Text = "● Not running", Foreground = MutedBrush };
		_statusTime = new TextBlock { Foreground = GreenBrush, IsVisible = false };

		_btnStart = Btn("Start", OnStart, cls: "green");
		_btnStop = Btn("Stop", OnStop, false, "red");

		Grid btnRow = new();
		btnRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
		btnRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		StackPanel btnPair = Row(8, _btnStart, _btnStop);
		TextBlock verLabel = new()
		{
			Text = $"{AppVersion}  ·  {DbCount:N0} IDs  ·  {DbUpdated}",
			FontSize = 10,
			Opacity = 0.25,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		Grid.SetColumn(verLabel, 1);
		btnRow.Children.Add(btnPair);
		btnRow.Children.Add(verLabel);

		StackPanel bottom = new()
		{
			Margin = new Thickness(14, 6, 14, 10),
			Spacing = 7,
			Children = { Row(10, _statusGame, _statusTime), btnRow }
		};

		_ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
		_ticker.Tick += (_, _) => UpdateElapsed();

		_game.TextChanged += (_, _) => OnGameTextChanged();

		Grid root = new();
		root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		Grid.SetRow(bottom, 1);
		root.Children.Add(_tabs);
		root.Children.Add(bottom);
		Content = root;
	}

	static RadioButton Radio(string text, bool on = false, string group = "t") => new()
		{ Content = text, IsChecked = on, GroupName = group };

	static TextBox Field(string placeholder) => new()
		{ Watermark = placeholder };

	static StackPanel Group(string label, Control content) => new()
	{
		Spacing = 3,
		Children = { new TextBlock { Text = label, FontSize = 11, Opacity = 0.55 }, content }
	};

	static Button Btn(string text, EventHandler<RoutedEventArgs> handler, bool enabled = true, string? cls = null)
	{
		Button b = new() { Content = text, IsEnabled = enabled, Padding = new Thickness(14, 7) };
		if (cls != null) b.Classes.Add(cls);
		b.Click += handler;
		return b;
	}

	static StackPanel Row(double spacing, params Control[] controls)
	{
		StackPanel p = new() { Orientation = Orientation.Horizontal, Spacing = spacing };
		foreach (Control c in controls) p.Children.Add(c);
		return p;
	}

	static StackPanel Stack(double margin, double spacing, params Control[] controls)
	{
		StackPanel p = new() { Margin = new Thickness(margin), Spacing = spacing };
		foreach (Control c in controls) p.Children.Add(c);
		return p;
	}

	static Grid Split(Control a, Control b)
	{
		Grid g = new();
		g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		Grid.SetColumn(b, 1);
		b.Margin = new Thickness(6, 0, 0, 0);
		g.Children.Add(a);
		g.Children.Add(b);
		return g;
	}

	Grid MakeGameRow()
	{
		Grid g = new();
		g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
		Grid.SetColumn(_xBtn, 1);
		_xBtn.Margin = new Thickness(4, 0, 0, 0);
		g.Children.Add(_game);
		g.Children.Add(_xBtn);
		return g;
	}

	void OnGameTextChanged()
	{
		string name = _game.Text?.Trim() ?? "";
		bool valid = GameDb.Db.ContainsKey(name);
		_xBtn.IsVisible = name.Length > 0 && !valid;
		_btnUpdate.IsVisible = !_btnStart.IsEnabled && valid && name != _current.Name;
	}

	void OnStart(object? _, RoutedEventArgs e)
	{
		string appId;
		string name;
		if (_tabs.SelectedIndex == 2)
		{
			appId = _fxAppId.Text?.Trim() ?? "";
			if (string.IsNullOrEmpty(appId)) { SetStatus("App ID required.", false); return; }
			name = Trim(_fxDetails.Text) ?? appId;
			_current = (name, appId);
			_rpcSince = StampCustom();
			_ = int.TryParse(_fxPartySize.Text, out int pSize);
			_ = int.TryParse(_fxPartyMax.Text, out int pMax);
			_rpc.Set(appId, _rpcSince, _fxDetails.Text, _fxState.Text, _fxLargeKey.Text, _fxLargeText.Text, _fxSmallKey.Text, _fxSmallText.Text, pSize, pMax, _fxBtn1Label.Text, _fxBtn1Url.Text, _fxBtn2Label.Text, _fxBtn2Url.Text);
			_settings.SaveCustom(new CustomAppSettings
			{
				AppId = appId, Details = _fxDetails.Text ?? "", State = _fxState.Text ?? "",
				LargeKey = _fxLargeKey.Text ?? "", LargeText = _fxLargeText.Text ?? "",
				SmallKey = _fxSmallKey.Text ?? "", SmallText = _fxSmallText.Text ?? "",
				PartySize = _fxPartySize.Text ?? "", PartyMax = _fxPartyMax.Text ?? "",
				Btn1Label = _fxBtn1Label.Text ?? "", Btn1Url = _fxBtn1Url.Text ?? "",
				Btn2Label = _fxBtn2Label.Text ?? "", Btn2Url = _fxBtn2Url.Text ?? "",
			});
			_settings.AddRecent(name, appId, isCustom: true);
			RefreshRecentPanel();
		}
		else
		{
			name = _game.Text?.Trim() ?? "";
			if (!GameDb.Db.TryGetValue(name, out appId!)) { SetStatus("Pick a game first.", false); return; }
			_current = (name, appId);
			_rpcSince = Stamp();
			_rpc.Set(appId, _rpcSince);
			_settings.AddRecent(name, appId);
			RefreshRecentPanel();
		}
		SetStatus(name, true);
		_ticker.Start();
		_btnStart.IsEnabled = false;
		_btnStop.IsEnabled = true;
	}

	void OnStop(object? _, RoutedEventArgs e)
	{
		_rpc.Stop();
		_ticker.Stop();
		SetStatus("Not running", false);
		_btnStart.IsEnabled = true;
		_btnStop.IsEnabled = false;
		_btnUpdate.IsVisible = false;
	}

	void OnUpdate(object? _, RoutedEventArgs e)
	{
		string name = _game.Text?.Trim() ?? "";
		if (!GameDb.Db.TryGetValue(name, out string? id)) return;
		_current = (name, id);
		if (_rbNow.IsChecked == true) _rpcSince = DateTime.UtcNow;
		_rpc.Set(id, _rpcSince);
		SetStatus(name, true);
		_btnUpdate.IsVisible = false;
	}

	void UpdateElapsed()
	{
		if (!_rpcSince.HasValue) return;
		TimeSpan elapsed = DateTime.UtcNow - _rpcSince.Value;
		_statusTime.Text = elapsed.TotalHours >= 1
			? $"{(int)elapsed.TotalHours}:{elapsed:mm\\:ss}"
			: $"{(int)elapsed.TotalMinutes}:{elapsed:ss}";
	}

	DateTime? Stamp()
	{
		if (_rbNow.IsChecked == true) return DateTime.UtcNow;
		if (_rbLaunch.IsChecked == true) return _appLaunch;
		return null;
	}

	DateTime? StampCustom()
	{
		if (_fxRbStart.IsChecked == true) return DateTime.UtcNow;
		if (_fxRbOpen.IsChecked == true) return _appLaunch;
		return null;
	}

	void SetStatus(string text, bool active)
	{
		_statusGame.Text = active ? $"▶ {text}" : $"● {text}";
		_statusGame.Foreground = active ? GreenBrush : MutedBrush;
		_statusTime.IsVisible = active;
		if (active) UpdateElapsed();
		if (!active) _statusTime.Text = "";
	}

	void RefreshRecentPanel()
	{
		_recentPanel.Children.Clear();
		_btnClear.IsVisible = _settings.Recent.Count > 0;
		if (_settings.Recent.Count == 0)
		{
			_recentPanel.Children.Add(new TextBlock { Text = "No recent games yet.", Opacity = 0.45, FontSize = 12 });
			return;
		}
		foreach (RecentGame g in _settings.Recent)
		{
			RecentGame cap = g;
			Button b = new()
			{
				Content = cap.IsCustom ? $"▶ {cap.Name} · custom" : $"▶ {cap.Name}",
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				Padding = new Thickness(10, 6),
			};
			b.Classes.Add("recent-item");
			b.Click += (_, _) => FastStart(cap.Name, cap.Id, cap.IsCustom);
			_recentPanel.Children.Add(b);
		}
	}

	void FastStart(string name, string id, bool isCustom = false)
	{
		if (!_btnStart.IsEnabled) { _rpc.Stop(); _ticker.Stop(); }
		_current = (name, id);
		_rpcSince = DateTime.UtcNow;
		if (isCustom && _settings.Custom is CustomAppSettings cs && cs.AppId == id)
		{
			_ = int.TryParse(cs.PartySize, out int ps);
			_ = int.TryParse(cs.PartyMax, out int pm);
			_rpc.Set(id, _rpcSince, cs.Details, cs.State, cs.LargeKey, cs.LargeText, cs.SmallKey, cs.SmallText, ps, pm, cs.Btn1Label, cs.Btn1Url, cs.Btn2Label, cs.Btn2Url);
		}
		else
			_rpc.Set(id, _rpcSince);
		SetStatus(name, true);
		_ticker.Start();
		_btnStart.IsEnabled = false;
		_btnStop.IsEnabled = true;
	}

	static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
	protected override void OnClosed(EventArgs e) { _ticker.Stop(); _rpc.Dispose(); base.OnClosed(e); }
}