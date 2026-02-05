using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Soundboard.Client.Models;

namespace Soundboard.Client;

public sealed class SoundboardClient : ISoundboardClient
{
    private readonly HttpClient _http;
    private readonly Uri _wsUri;

    public SoundboardClient(string baseUrl = "http://localhost:8765")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _wsUri = new Uri(baseUrl.Replace("http", "ws") + "/stream");
    }

    public async Task<EngineInfo> GetHealthAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<EngineInfo>(
            "/api/health",
            ct
        ) ?? throw new InvalidOperationException("Invalid health response");
    }

    public async Task<IReadOnlyList<string>> GetPresetsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<JsonElement>("/api/presets", ct);
        return response.GetProperty("presets").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetVoicesAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<JsonElement>("/api/voices", ct);
        return response.GetProperty("voices").EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()!)
            .ToList();
    }

    public async Task SpeakAsync(
        SpeakRequest request,
        IProgress<AudioChunk> audioProgress,
        CancellationToken ct = default)
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(_wsUri, ct);

        var message = JsonSerializer.Serialize(new
        {
            type = "speak",
            request_id = Guid.NewGuid().ToString(),
            payload = request
        });

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(message),
            WebSocketMessageType.Text,
            true,
            ct
        );

        var buffer = new byte[8192];

        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var json = JsonDocument.Parse(new ReadOnlyMemory<byte>(buffer, 0, result.Count));
            if (json.RootElement.GetProperty("type").GetString() == "audio_chunk")
            {
                var payload = json.RootElement.GetProperty("payload");
                var pcm = Convert.FromBase64String(payload.GetProperty("data").GetString()!);
                var rate = payload.GetProperty("sample_rate").GetInt32();

                audioProgress.Report(new AudioChunk(pcm, rate));
            }
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        await _http.PostAsync("/api/stop", null, ct);
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
