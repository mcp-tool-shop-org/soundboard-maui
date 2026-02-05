namespace Soundboard.Client;

public sealed record SoundboardClientOptions
{
    private const string DefaultBaseUrl = "http://localhost:8765";
    private const string EnvVarName = "SOUNDBOARD_BASE_URL";

    public string BaseUrl { get; init; } =
        Environment.GetEnvironmentVariable(EnvVarName) ?? DefaultBaseUrl;

    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan WebSocketConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

    internal Uri WsUri => new(BaseUrl.Replace("http", "ws") + "/stream");
}
