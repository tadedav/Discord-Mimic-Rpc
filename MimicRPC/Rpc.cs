using DiscordRPC;

sealed class Rpc : IDisposable
{
	DiscordRpcClient? _client;

	public void Set(string appId, DateTime? since,
		string? details = null, string? state = null,
		string? largeKey = null, string? largeText = null,
		string? smallKey = null, string? smallText = null,
		int partyCurrent = 0, int partyMax = 0,
		string? btn1Label = null, string? btn1Url = null,
		string? btn2Label = null, string? btn2Url = null)
	{
		if (_client?.ApplicationID != appId)
		{
			_client?.Dispose();
			_client = new DiscordRpcClient(appId);
			_client.Initialize();
		}

		Assets? assets = null;
		if (largeKey != null || smallKey != null)
			assets = new Assets
			{
				LargeImageKey = largeKey, LargeImageText = largeText,
				SmallImageKey = smallKey, SmallImageText = smallText
			};

		Party? party = (partyCurrent > 0 && partyMax > 0)
			? new Party { ID = appId, Size = partyCurrent, Max = partyMax }
			: null;

		List<Button> btns = [];
		if (!string.IsNullOrWhiteSpace(btn1Label) && !string.IsNullOrWhiteSpace(btn1Url))
			btns.Add(new Button { Label = btn1Label.Trim(), Url = btn1Url.Trim() });
		if (!string.IsNullOrWhiteSpace(btn2Label) && !string.IsNullOrWhiteSpace(btn2Url))
			btns.Add(new Button { Label = btn2Label.Trim(), Url = btn2Url.Trim() });

		_client.SetPresence(new RichPresence
		{
			Details = Trim(details),
			State = Trim(state),
			Timestamps = since.HasValue ? new Timestamps(since.Value) : null,
			Assets = assets,
			Party = party,
			Buttons = btns.Count > 0 ? btns.ToArray() : null
		});
	}

	public void Clear() => _client?.ClearPresence();
	public void Stop() { _client?.ClearPresence(); _client?.Dispose(); _client = null; }
	public void Dispose() => Stop();

	static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}