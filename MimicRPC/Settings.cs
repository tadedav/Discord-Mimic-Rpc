using System.Text.Json;

sealed class Settings
{
	static readonly string _path = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"MimicRPC", "settings.json");

	public List<RecentGame> Recent { get; set; } = [];
	public CustomAppSettings? Custom { get; set; }

	public static Settings Load()
	{
		try
		{
			if (File.Exists(_path))
				return JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path)) ?? new();
		}
		catch { }
		return new();
	}

	public void AddRecent(string name, string id, bool isCustom = false)
	{
		Recent.RemoveAll(r => r.Id == id);
		Recent.Insert(0, new RecentGame { Name = name, Id = id, IsCustom = isCustom });
		if (Recent.Count > 5) Recent.RemoveRange(5, Recent.Count - 5);
		Save();
	}

	public void SaveCustom(CustomAppSettings data) { Custom = data; Save(); }

	public void ClearCustom() { Custom = null; Save(); }

	public void ClearAll()
	{
		try { if (File.Exists(_path)) File.Delete(_path); } catch { }
		Recent.Clear();
		Custom = null;
	}

	void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
			File.WriteAllText(_path, JsonSerializer.Serialize(this,
				new JsonSerializerOptions { WriteIndented = true }));
		}
		catch { }
	}
}

sealed class RecentGame
{
	public string Name { get; set; } = "";
	public string Id { get; set; } = "";
	public bool IsCustom { get; set; }
}

sealed class CustomAppSettings
{
	public string AppId { get; set; } = "";
	public string Details { get; set; } = "";
	public string State { get; set; } = "";
	public string LargeKey { get; set; } = "";
	public string LargeText { get; set; } = "";
	public string SmallKey { get; set; } = "";
	public string SmallText { get; set; } = "";
	public string PartySize { get; set; } = "";
	public string PartyMax { get; set; } = "";
	public string Btn1Label { get; set; } = "";
	public string Btn1Url { get; set; } = "";
	public string Btn2Label { get; set; } = "";
	public string Btn2Url { get; set; } = "";
}
