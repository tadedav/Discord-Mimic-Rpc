using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.Runtime.InteropServices;

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
	readonly Button _btnClear = new() { Content = "Clear history", Padding = new Thickness(8, 4), HorizontalAlignment = HorizontalAlignment.Left };
	readonly TextBlock _updateLabel = new() { FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, IsVisible = false, Background = Brushes.Transparent };
	readonly TextBlock _recentHeader = new() { FontSize = 12, Opacity = 0.55 };
	readonly CheckBox _chkShowTime;
	readonly CheckBox _chkMinimizeToTray = new() { Content = "Minimize to tray on close", FontSize = 12 };
	readonly CheckBox _chkRunAtStartup = new() { Content = "Run at system startup", FontSize = 12 };
	readonly CheckBox _chkAlwaysOnTop = new() { Content = "Always on top", FontSize = 12 };
	readonly CheckBox _chkAutoStartLast = new() { Content = "Auto-start last session", FontSize = 12 };
	readonly TextBlock _autoStartLastHint = new() { FontSize = 10, Opacity = 0.4, IsVisible = false, Margin = new Thickness(22, 0, 0, 0) };
	readonly Button _btnClearCustom = new() { Content = "Clear custom app", Padding = new Thickness(8, 4), HorizontalAlignment = HorizontalAlignment.Left };
	readonly Button _btnClearSettings = new() { Content = "Clear settings", Padding = new Thickness(8, 4), HorizontalAlignment = HorizontalAlignment.Left };
	readonly TrayIcon _tray = new();
	readonly NativeMenuItem _trayOpen;
	readonly NativeMenuItem _trayExit;
	bool _isExiting;
	string? _updateUrl;

	static readonly SolidColorBrush GreenBrush = new(Color.Parse("#3BA55D"));
	static readonly SolidColorBrush MutedBrush = new(Color.Parse("#949BA4"));

	public MainWindow()
	{
		Title = "MimicRPC";
		Width = 380;
		CanResize = false;
		SizeToContent = SizeToContent.Height;
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
		HoverStyle(_xBtn, "#FF5555", "#C03537");

		_rbNow = Radio("Since Start", true);
		_rbLaunch = Radio("Since Open");
		_fxRbStart = Radio("Since Start", true, "cx");
		_fxRbOpen = Radio("Since Open", false, "cx");

		_btnUpdate = new Button { Content = "Update", IsVisible = false, Padding = new Thickness(12, 6) };
		_btnUpdate.Classes.Add("orange");
		_btnUpdate.Click += OnUpdate;
		HoverStyle(_btnUpdate, "#FFA322", "#C47009");

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

		foreach (TextBox fxBox in new[] { _fxAppId, _fxDetails, _fxState, _fxLargeKey, _fxLargeText, _fxSmallKey, _fxSmallText, _fxPartySize, _fxPartyMax, _fxBtn1Label, _fxBtn1Url, _fxBtn2Label, _fxBtn2Url })
			fxBox.TextChanged += (_, _) => RefreshSettingsButtons();

		_btnClear.Classes.Add("ghost-red");
		_btnClear.Click += (_, _) =>
		{
			_settings.ClearHistory();
			RefreshRecentPanel();
		};
		HoverStyle(_btnClear, "#33ED4245", "#4DED4245", "#CCED4245", "#CCED4245");

		RefreshRecentPanel();

		ScrollViewer welcomeContent = new()
		{
			MaxHeight = 280,
			Content = Stack(14, 12,
				new TextBlock { Text = "Welcome to MimicRPC", FontSize = 13, FontWeight = FontWeight.SemiBold },
				_recentHeader,
				_recentPanel
			)
		};

		_btnClearCustom.Classes.Add("ghost-red");
		HoverStyle(_btnClearCustom, "#33ED4245", "#4DED4245", "#CCED4245", "#CCED4245");
		_btnClearCustom.Click += (_, _) =>
		{
			_settings.ClearCustom();
			_fxAppId.Text = _fxDetails.Text = _fxState.Text = null;
			_fxLargeKey.Text = _fxLargeText.Text = _fxSmallKey.Text = _fxSmallText.Text = null;
			_fxPartySize.Text = _fxPartyMax.Text = null;
			_fxBtn1Label.Text = _fxBtn1Url.Text = _fxBtn2Label.Text = _fxBtn2Url.Text = null;
			RefreshSettingsButtons();
		};
		_btnClearSettings.Classes.Add("ghost-red");
		HoverStyle(_btnClearSettings, "#33ED4245", "#4DED4245", "#CCED4245", "#CCED4245");
		_btnClearSettings.Click += (_, _) =>
		{
			_settings.ClearPreferences();
			_chkShowTime!.IsChecked = _settings.ShowTime;
			_chkMinimizeToTray.IsChecked = _settings.MinimizeToTray;
			_chkAlwaysOnTop.IsChecked = _settings.AlwaysOnTop;
			_chkAutoStartLast.IsChecked = false;
			Topmost = false;
			StartupHelper.SetEnabled(false);
			_chkRunAtStartup.IsChecked = false;
			RefreshSettingsButtons();
		};

		_chkShowTime = new CheckBox { Content = "Show elapsed time", IsChecked = _settings.ShowTime, FontSize = 12 };
		_chkShowTime.IsCheckedChanged += (_, _) =>
		{
			_settings.SetShowTime(_chkShowTime.IsChecked == true);
			_statusTime!.IsVisible = _settings.ShowTime && !_btnStart!.IsEnabled;
			RefreshSettingsButtons();
		};
		_chkMinimizeToTray.IsChecked = _settings.MinimizeToTray;
		_chkMinimizeToTray.IsCheckedChanged += (_, _) =>
		{
			_settings.SetMinimizeToTray(_chkMinimizeToTray.IsChecked == true);
			_settings.SetHasAskedAboutTray(true);
			RefreshSettingsButtons();
		};

		_chkRunAtStartup.IsChecked = StartupHelper.GetEnabled();
		_chkRunAtStartup.IsCheckedChanged += (_, _) => { StartupHelper.SetEnabled(_chkRunAtStartup.IsChecked == true); RefreshSettingsButtons(); };
		_chkAlwaysOnTop.IsChecked = _settings.AlwaysOnTop;
		_chkAlwaysOnTop.IsCheckedChanged += (_, _) => { _settings.SetAlwaysOnTop(_chkAlwaysOnTop.IsChecked == true); Topmost = _chkAlwaysOnTop.IsChecked == true; RefreshSettingsButtons(); };
		_chkAutoStartLast.IsChecked = _settings.AutoStartLast;
		_chkAutoStartLast.IsCheckedChanged += (_, _) => { _settings.SetAutoStartLast(_chkAutoStartLast.IsChecked == true); RefreshSettingsButtons(); };

		StackPanel settingsContent = Stack(14, 10,
			Stack(0, 4, _chkShowTime, _chkMinimizeToTray, _chkRunAtStartup, _chkAlwaysOnTop, _chkAutoStartLast, _autoStartLastHint),
			Stack(0, 6, _btnClearSettings, _btnClear, _btnClearCustom)
		);

		_tabs = new TabControl();
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Welcome", FontSize = 13 }, Content = welcomeContent });
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Game", FontSize = 13 }, Content = gameContent });
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Custom", FontSize = 13 }, Content = customContent });
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Settings", FontSize = 13 }, Content = settingsContent });
		_ = _tabs.Items.Add(new TabItem { Header = new TextBlock { Text = "Changelog", FontSize = 13 }, Content = BuildChangelog() });
		_tabs.SelectionChanged += (_, _) => UpdateWindowHeight();
		Opened += (_, _) =>
		{
			Topmost = _settings.AlwaysOnTop;
			Activate();
			Dispatcher.UIThread.InvokeAsync(() =>
			{
				UpdateWindowHeight();
				if (_settings.AutoStartLast && _settings.Recent.Count > 0)
					FastStart(_settings.Recent[0].Name, _settings.Recent[0].Id, _settings.Recent[0].IsCustom);
			}, DispatcherPriority.Background);
		};

		_statusGame = new TextBlock { Text = "● Not running", Foreground = MutedBrush };
		_statusTime = new TextBlock { Foreground = GreenBrush, IsVisible = false };

		_btnStart = Btn("Start", OnStart, cls: "green");
		HoverStyle(_btnStart, "#3BA55D", "#1D6B38");
		_btnStop = Btn("Stop", OnStop, false, "stop");
		HoverStyle(_btnStop, "#FF5555", "#C03537");

		Grid btnRow = new();
		btnRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
		btnRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		StackPanel btnPair = Row(8, _btnStart, _btnStop);
		btnPair.VerticalAlignment = VerticalAlignment.Bottom;
		TextBlock githubLabel = new()
		{
			Text = "↗ GitHub",
			FontSize = 10,
			Opacity = 0.3,
			HorizontalAlignment = HorizontalAlignment.Right,
			Cursor = new Cursor(StandardCursorType.Hand),
			Background = Brushes.Transparent
		};
		githubLabel.PointerPressed += (_, _) => OpenUrl($"https://github.com/{AppInfo.GithubOwner}/{AppInfo.GithubRepoName}");
		githubLabel.PointerEntered += (_, _) => githubLabel.TextDecorations = TextDecorations.Underline;
		githubLabel.PointerExited += (_, _) => githubLabel.TextDecorations = null;
		TextBlock verLabel = new()
		{
			Text = $"{AppInfo.Version}  ·  {GameDb.Db.Count:N0} IDs  ·  {AppInfo.DbUpdated}",
			FontSize = 10,
			Opacity = 0.25,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		StackPanel rightStack = new()
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Spacing = 2,
			Children = { _updateLabel, githubLabel, verLabel }
		};
		Grid.SetColumn(rightStack, 1);
		btnRow.Children.Add(btnPair);
		btnRow.Children.Add(rightStack);

		StackPanel bottom = new()
		{
			Margin = new Thickness(14, 6, 14, 10),
			Spacing = 7,
			Children = { Row(10, _statusGame, _statusTime), btnRow }
		};

		_ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
		_ticker.Tick += (_, _) => UpdateElapsed();

		_game.TextChanged += (_, _) => OnGameTextChanged();
		_ = CheckForUpdates();

		_trayOpen = new() { Header = "Open MimicRPC" };
		_trayOpen.Click += (_, _) => ShowFromTray();
		_trayExit = new() { Header = "Exit" };
		_trayExit.Click += (_, _) =>
		{
			_isExiting = true;
			_tray.IsVisible = false;
			(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
		};
		_tray.Menu = new NativeMenu();
		_tray.ToolTipText = "MimicRPC";
		_tray.Icon = TryLoadTrayIcon();
		_tray.Clicked += (_, _) => ShowFromTray();
		TrayIcon.SetIcons(Application.Current!, [_tray]);
		RefreshTrayMenu();
		RefreshSettingsButtons();

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
			_settings.AddRecent(name, appId, isCustom: true, partySize: pSize > 0 ? pSize : null, partyMax: pMax > 0 ? pMax : null);
		}
		else
		{
			name = _game.Text?.Trim() ?? "";
			if (!GameDb.Db.TryGetValue(name, out appId!)) { SetStatus("Pick a game first.", false); return; }
			_current = (name, appId);
			_rpcSince = Stamp();
			_rpc.Set(appId, _rpcSince);
			_settings.AddRecent(name, appId);
		}
		SetStatus(name, true);
		_ticker.Start();
		_btnStart.IsEnabled = false;
		_btnStop.IsEnabled = true;
		RefreshRecentPanel();
	}

	void OnStop(object? _, RoutedEventArgs e)
	{
		_rpc.Stop();
		_ticker.Stop();
		SetStatus("Not running", false);
		_btnStart.IsEnabled = true;
		_btnStop.IsEnabled = false;
		_btnUpdate.IsVisible = false;
		RefreshRecentPanel();
	}

	void OnUpdate(object? _, RoutedEventArgs e)
	{
		string name = _game.Text?.Trim() ?? "";
		if (!GameDb.Db.TryGetValue(name, out string? id)) return;
		_current = (name, id);
		if (_rbNow.IsChecked == true) _rpcSince = DateTime.UtcNow;
		_rpc.Set(id, _rpcSince);
		_settings.AddRecent(name, id);
		SetStatus(name, true);
		_btnUpdate.IsVisible = false;
		RefreshRecentPanel();
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
		_statusTime.IsVisible = active && _settings.ShowTime;
		if (active) UpdateElapsed();
		if (!active) _statusTime.Text = "";
	}

	void RefreshRecentPanel()
	{
		_recentPanel.Children.Clear();
		_recentHeader.Text = _settings.Recent.Count > 0 ? $"Recent ({_settings.Recent.Count}/5)" : "Recent";
		RefreshSettingsButtons();
		if (_settings.Recent.Count == 0)
		{
			_recentPanel.Children.Add(new TextBlock { Text = "No recent games yet.", Opacity = 0.45, FontSize = 12 });
			UpdateWindowHeight();
			return;
		}
		foreach (RecentGame g in _settings.Recent)
		{
			RecentGame cap = g;
			bool active = _btnStart is { IsEnabled: false } && cap.Id == _current.Id;
			string partySuffix = cap.PartySize is > 0 && cap.PartyMax is > 0
				? $" · {cap.PartySize}/{cap.PartyMax}" : "";
			Button b = new()
			{
				Content = cap.IsCustom ? $"▶ {cap.Name} · custom{partySuffix}" : $"▶ {cap.Name}{partySuffix}",
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				Padding = new Thickness(10, 6),
			};
			b.Classes.Add(active ? "recent-item-active" : "recent-item");
			if (!active) b.Click += (_, _) => FastStart(cap.Name, cap.Id, cap.IsCustom);
			_recentPanel.Children.Add(b);
		}
		UpdateWindowHeight();
		RefreshTrayMenu();
	}

	void RefreshTrayMenu()
	{
		NativeMenu? menu = _tray.Menu;
		if (menu is null || _trayOpen is null) return;
		menu.Items.Clear();
		menu.Items.Add(_trayOpen);
		menu.Items.Add(new NativeMenuItemSeparator());
		foreach (RecentGame g in _settings.Recent)
		{
			RecentGame cap = g;
			bool active = _btnStart is { IsEnabled: false } && cap.Id == _current.Id;
			NativeMenuItem item = new() { Header = active ? $"● {cap.Name}" : $"▶ {cap.Name}" };
			item.Click += (_, _) => FastStart(cap.Name, cap.Id, cap.IsCustom);
			menu.Items.Add(item);
		}
		if (_settings.Recent.Count > 0)
			menu.Items.Add(new NativeMenuItemSeparator());
		menu.Items.Add(_trayExit);
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
		RefreshRecentPanel();
	}

	static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

	void UpdateWindowHeight()
	{
		if (!IsVisible || WindowState != WindowState.Normal) return;
		SizeToContent = SizeToContent.Manual;   // force change event even if already Height
		SizeToContent = SizeToContent.Height;
		Dispatcher.UIThread.InvokeAsync(() => SizeToContent = SizeToContent.Manual, DispatcherPriority.Background);
	}

	protected override void OnClosed(EventArgs e) { _ticker.Stop(); _rpc.Dispose(); _tray.IsVisible = false; base.OnClosed(e); }

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (_isExiting) { base.OnClosing(e); return; }
		if (!_settings.HasAskedAboutTray)
		{
			e.Cancel = true;
			_ = PromptTrayPreference();
			return;
		}
		if (_settings.MinimizeToTray)
		{
			e.Cancel = true;
			HideToTray();
			return;
		}
		base.OnClosing(e);
	}

	static void HoverStyle(Button b, string hover, string press, string? fg = null, string? border = null)
	{
		b.Cursor = new Cursor(StandardCursorType.Hand);
		b.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Color.Parse(hover));
		b.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Color.Parse(press));
		if (fg != null)
		{
			b.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Color.Parse(fg));
			b.Resources["ButtonForegroundPressed"] = new SolidColorBrush(Color.Parse(fg));
		}
		if (border != null)
		{
			b.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Color.Parse(border));
			b.Resources["ButtonBorderBrushPressed"] = new SolidColorBrush(Color.Parse(border));
		}
	}

	static void OpenUrl(string url)
	{
		try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
		catch { }
	}

	void HideToTray() { SetMacDockPolicy(false); ShowInTaskbar = false; Hide(); }
	void ShowFromTray()
	{
		ShowInTaskbar = true;
		SetMacDockPolicy(true);
		Show();
		WindowState = WindowState.Normal;
		Activate();
		Dispatcher.UIThread.InvokeAsync(UpdateWindowHeight, DispatcherPriority.Background);
	}

	void RefreshSettingsButtons()
	{
		bool h = _settings.Recent.Count > 0;
		bool c = _settings.Custom != null || HasCustomContent();
		bool s = _settings.MinimizeToTray || !_settings.ShowTime || _settings.AlwaysOnTop || StartupHelper.GetEnabled() || _settings.AutoStartLast;
		bool hint = _chkAutoStartLast.IsChecked == true;
		bool changed = _btnClear.IsVisible != h || _btnClearCustom.IsVisible != c || _btnClearSettings.IsVisible != s || _autoStartLastHint.IsVisible != hint;
		_btnClear.IsVisible = h;
		_btnClearCustom.IsVisible = c;
		_btnClearSettings.IsVisible = s;
		UpdateAutoStartHint();
		if (changed) UpdateWindowHeight();
	}

	bool HasCustomContent() =>
		!string.IsNullOrWhiteSpace(_fxAppId.Text) ||
		!string.IsNullOrWhiteSpace(_fxDetails.Text) ||
		!string.IsNullOrWhiteSpace(_fxState.Text) ||
		!string.IsNullOrWhiteSpace(_fxLargeKey.Text) ||
		!string.IsNullOrWhiteSpace(_fxSmallKey.Text) ||
		!string.IsNullOrWhiteSpace(_fxBtn1Label.Text) ||
		!string.IsNullOrWhiteSpace(_fxBtn2Label.Text);

	void UpdateAutoStartHint()
	{
		bool on = _chkAutoStartLast.IsChecked == true;
		_autoStartLastHint.IsVisible = on;
		if (on) _autoStartLastHint.Text = _settings.Recent.Count > 0 ? $"Will resume: {_settings.Recent[0].Name}" : "No recent session";
	}

	[DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
	static extern nint ObjcMsg(nint r, nint s);
	[DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
	static extern void ObjcMsgPolicy(nint r, nint s, long p);
	[DllImport("libobjc.dylib")]
	static extern nint objc_getClass(string n);
	[DllImport("libobjc.dylib")]
	static extern nint sel_getUid(string n);
	static void SetMacDockPolicy(bool show)
	{
		if (!OperatingSystem.IsMacOS()) return;
		nint app = ObjcMsg(objc_getClass("NSApplication"), sel_getUid("sharedApplication"));
		ObjcMsgPolicy(app, sel_getUid("setActivationPolicy:"), show ? 0L : 1L);
	}

	async Task PromptTrayPreference()
	{
		bool keepInTray = await ShowTrayQuestion();
		_settings.SetMinimizeToTray(keepInTray);
		_settings.SetHasAskedAboutTray(true);
		_chkMinimizeToTray.IsChecked = keepInTray;
		RefreshSettingsButtons();
		if (keepInTray)
			HideToTray();
		else
		{
			_isExiting = true;
			Close();
		}
	}

	async Task<bool> ShowTrayQuestion()
	{
		bool result = false;
		Window dialog = new()
		{
			Title = "MimicRPC",
			Width = 320,
			CanResize = false,
			SizeToContent = SizeToContent.Height,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Background = new SolidColorBrush(Color.Parse("#11141C")),
		};
		Button btnClose = new() { Content = "Close completely", Padding = new Thickness(12, 7) };
		Button btnTray = new() { Content = "Keep in tray", Padding = new Thickness(12, 7) };
		btnTray.Classes.Add("accent");
		btnClose.Click += (_, _) => { result = false; dialog.Close(); };
		btnTray.Click += (_, _) => { result = true; dialog.Close(); };
		dialog.Content = new StackPanel
		{
			Margin = new Thickness(16),
			Spacing = 14,
			Children =
			{
				new TextBlock { Text = "Keep running in the system tray when closed?", FontSize = 13, TextWrapping = TextWrapping.Wrap },
				new TextBlock { Text = "You can change this later in Settings.", FontSize = 11, Opacity = 0.5 },
				new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 8,
					HorizontalAlignment = HorizontalAlignment.Right,
					Children = { btnClose, btnTray }
				}
			}
		};
		await dialog.ShowDialog(this);
		return result;
	}

	static ScrollViewer BuildChangelog()
	{
		static StackPanel Entry(string version, string date, string[] items)
		{
			StackPanel p = new() { Spacing = 3 };
			p.Children.Add(new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8,
				Children =
				{
					new TextBlock { Text = version, FontSize = 12, FontWeight = FontWeight.SemiBold },
					new TextBlock { Text = date, FontSize = 11, Opacity = 0.4, VerticalAlignment = VerticalAlignment.Center }
				}
			});
			foreach (string item in items)
				p.Children.Add(new TextBlock { Text = $"• {item}", FontSize = 11, Opacity = 0.65, TextWrapping = TextWrapping.Wrap });
			return p;
		}

		return new ScrollViewer
		{
			MaxHeight = 280,
			Content = Stack(14, 14,
				Entry("Beta 0.6", "May 2026", [
					"Recent games in system tray",
					"Fixed app icon on macOS",
					"Fixed window height after restoring from tray"
				]),
				Entry("Beta 0.5", "May 2026", [
					"Auto-start last session, always on top",
					"Run at system startup, update check",
					"System tray with dock icon support",
					"Fixed minimize button on macOS Sequoia",
					"Various UI improvements and bug fixes"
				]),
				Entry("Beta 0.3", "May 2026", [
					"Discord Rich Presence with 22,900+ game IDs",
					"Custom App ID with full RPC control",
					"Recent games history"
				])
			)
		};
	}

	async Task CheckForUpdates()
	{
		try
		{
			using HttpClientHandler handler = new() { AllowAutoRedirect = false };
			using HttpClient http = new(handler);
			http.Timeout = TimeSpan.FromSeconds(5);
			http.DefaultRequestHeaders.UserAgent.ParseAdd($"MimicRPC/{AppInfo.VersionTag}");
			HttpResponseMessage res = await http.GetAsync(
				$"https://github.com/{AppInfo.GithubOwner}/{AppInfo.GithubRepoName}/releases/latest");
			string loc = res.Headers.Location?.ToString() ?? "";
			string tag = loc.Contains("/releases/tag/") ? loc.Split("/releases/tag/").Last() : "";
			if (string.IsNullOrEmpty(tag)) throw new Exception();
			string latestNum = tag.TrimStart('v');
			string currentNum = AppInfo.VersionTag;
			bool upToDate = !Version.TryParse(latestNum, out Version? latest)
				|| !Version.TryParse(currentNum, out Version? current)
				|| latest <= current;
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (upToDate)
				{
					_updateLabel.Text = "Up to date";
					_updateLabel.Foreground = MutedBrush;
				}
				else
				{
					_updateUrl = $"https://github.com/{AppInfo.GithubOwner}/{AppInfo.GithubRepoName}/releases/latest";
					_updateLabel.Text = $"↑ {tag} available";
					_updateLabel.Foreground = new SolidColorBrush(Color.Parse("#F0890C"));
					_updateLabel.Cursor = new Cursor(StandardCursorType.Hand);
					_updateLabel.PointerPressed += (_, _) => OpenUrl(_updateUrl);
				}
				_updateLabel.IsVisible = true;
			});
		}
		catch
		{
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				_updateLabel.Text = "Failed to check updates";
				_updateLabel.Foreground = MutedBrush;
				_updateLabel.IsVisible = true;
			});
		}
	}

	static WindowIcon? TryLoadTrayIcon()
	{
		try
		{
			using Stream? s = typeof(MainWindow).Assembly.GetManifestResourceStream("tray.png");
			return s != null ? new WindowIcon(s) : null;
		}
		catch { return null; }
	}
}