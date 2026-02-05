using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Soundboard.Client.Models;

namespace Soundboard.Client;

public sealed class SoundboardClient : ISoundboardClient
{
    private readonly HttpClient _http;
    private readonly SoundboardClientOptions _options;
    private readonly ILogger _logger;

    public SoundboardClient(
        SoundboardClientOptions? options = null,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        _options = options ?? new SoundboardClientOptions();
        _logger = logger ?? NullLogger.Instance;
        _http = httpClient ?? new HttpClient();
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = _options.HttpTimeout;
        _logger.LogDebug("SoundboardClient created: base={BaseUrl}, httpTimeout={Timeout}s",
            _options.BaseUrl, _options.HttpTimeout.TotalSeconds);
    }

    internal SoundboardClient(HttpClient http, SoundboardClientOptions options, ILogger? logger = null)
    {
        _http = http;
        _options = options;
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task<EngineInfo> GetHealthAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("GET /api/health");
        return await _http.GetFromJsonAsync<EngineInfo>(
            "/api/health",
            ct
        ) ?? throw new InvalidOperationException("Invalid health response");
    }

    public async Task<IReadOnlyList<string>> GetPresetsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("GET /api/presets");
        var response = await _http.GetFromJsonAsync<JsonElement>("/api/presets", ct);
        var presets = response.GetProperty("presets").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToList();
        _logger.LogDebug("Received {Count} presets", presets.Count);
        return presets;
    }

    public async Task<IReadOnlyList<string>> GetVoicesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("GET /api/voices");
        var response = await _http.GetFromJsonAsync<JsonElement>("/api/voices", ct);
        var voices = response.GetProperty("voices").EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()!)
            .ToList();
        _logger.LogDebug("Received {Count} voices", voices.Count);
        return voices;
    }

    public async Task SpeakAsync(
        SpeakRequest request,
        IProgress<AudioChunk> audioProgress,
        CancellationToken ct = default)
    {
        using var ws = new ClientWebSocket();

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(_options.WebSocketConnectTimeout);

        _logger.LogDebug("Connecting WebSocket to {Uri}", _options.WsUri);
        await ws.ConnectAsync(_options.WsUri, connectCts.Token);

        var requestId = request.ResolvedRequestId;
        _logger.LogInformation("SpeakAsync started: requestId={RequestId}, text={TextLength}chars",
            requestId, request.Text.Length);

        var message = JsonSerializer.Serialize(new
        {
            type = "speak",
            request_id = requestId,
            payload = new { text = request.Text, preset = request.Preset, voice = request.Voice }
        });

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(message),
            WebSocketMessageType.Text,
            true,
            ct
        );

        var buffer = new byte[8192];
        using var msgStream = new MemoryStream();
        var chunkCount = 0;

        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            using var receiveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            receiveCts.CancelAfter(_options.WebSocketReceiveTimeout);

            var result = await ws.ReceiveAsync(buffer, receiveCts.Token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogDebug("WebSocket closed by server");
                break;
            }

            msgStream.Write(buffer, 0, result.Count);

            if (!result.EndOfMessage)
                continue;

            using var json = JsonDocument.Parse(msgStream.ToArray());
            msgStream.SetLength(0);

            var msgType = json.RootElement.GetProperty("type").GetString();

            if (msgType == "audio_chunk")
            {
                var payload = json.RootElement.GetProperty("payload");
                var pcm = Convert.FromBase64String(payload.GetProperty("data").GetString()!);
                var rate = payload.GetProperty("sample_rate").GetInt32();

                chunkCount++;
                audioProgress.Report(new AudioChunk(pcm, rate));
            }
            else if (msgType == "state")
            {
                var state = json.RootElement.GetProperty("payload")
                    .GetProperty("state").GetString();
                _logger.LogDebug("State event: {State} (requestId={RequestId})", state, requestId);
                if (state == "finished")
                    break;
            }
            else if (msgType == "error")
            {
                var errorMsg = json.RootElement.GetProperty("payload")
                    .GetProperty("message").GetString() ?? "Engine error";
                _logger.LogError("Engine error: {Message} (requestId={RequestId})", errorMsg, requestId);
                throw new InvalidOperationException(errorMsg);
            }
        }

        _logger.LogInformation("SpeakAsync completed: requestId={RequestId}, chunks={ChunkCount}",
            requestId, chunkCount);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("POST /api/stop");
        await _http.PostAsync("/api/stop", null, ct);
    }

    public ValueTask DisposeAsync()
    {
        _logger.LogDebug("SoundboardClient disposing");
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
