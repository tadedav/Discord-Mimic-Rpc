using System.Runtime.Versioning;

static class StartupHelper
{
	const string AppName = "MimicRPC";
	const string MacPlistLabel = "com.mimicropc";
	static string ExePath => Environment.ProcessPath ?? string.Empty;

	public static bool GetEnabled()
	{
		if (OperatingSystem.IsWindows()) return GetWindowsStartup();
		if (OperatingSystem.IsMacOS()) return GetMacOSStartup();
		if (OperatingSystem.IsLinux()) return GetLinuxStartup();
		return false;
	}

	public static void SetEnabled(bool enable)
	{
		if (OperatingSystem.IsWindows()) SetWindowsStartup(enable);
		else if (OperatingSystem.IsMacOS()) SetMacOSStartup(enable);
		else if (OperatingSystem.IsLinux()) SetLinuxStartup(enable);
	}

	[SupportedOSPlatform("windows")]
	static bool GetWindowsStartup()
	{
		try
		{
			using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
			return key?.GetValue(AppName) != null;
		}
		catch { return false; }
	}

	[SupportedOSPlatform("windows")]
	static void SetWindowsStartup(bool enable)
	{
		try
		{
			using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
			if (key == null) return;
			if (enable) key.SetValue(AppName, $"\"{ExePath}\"");
			else key.DeleteValue(AppName, throwOnMissingValue: false);
		}
		catch { }
	}

	static string MacPlistPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $"Library/LaunchAgents/{MacPlistLabel}.plist");
	[SupportedOSPlatform("macos")]
	static bool GetMacOSStartup() => File.Exists(MacPlistPath);
	[SupportedOSPlatform("macos")]
	static void SetMacOSStartup(bool enable)
	{
		try
		{
			string path = MacPlistPath;
			if (enable)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, $"""
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>Label</key>
	<string>{MacPlistLabel}</string>
	<key>ProgramArguments</key>
	<array>
		<string>{ExePath}</string>
	</array>
	<key>RunAtLoad</key>
	<true/>
	<key>KeepAlive</key>
	<false/>
</dict>
</plist>
""");
			}
			else if (File.Exists(path))
				File.Delete(path);
		}
		catch { }
	}

	static string LinuxDesktopPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/autostart/MimicRPC.desktop");
	static bool GetLinuxStartup() => File.Exists(LinuxDesktopPath);
	static void SetLinuxStartup(bool enable)
	{
		try
		{
			string path = LinuxDesktopPath;
			if (enable)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, $"""
[Desktop Entry]
Type=Application
Name=MimicRPC
Exec={ExePath}
X-GNOME-Autostart-enabled=true
Hidden=false
NoDisplay=false
""");
			}
			else if (File.Exists(path))
				File.Delete(path);
		}
		catch { }
	}
}
