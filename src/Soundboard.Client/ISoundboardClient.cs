using Soundboard.Client.Models;

namespace Soundboard.Client;

public interface ISoundboardClient : IAsyncDisposable
{
    Task<EngineInfo> GetHealthAsync(CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetPresetsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetVoicesAsync(CancellationToken ct = default);

    Task SpeakAsync(
        SpeakRequest request,
        IProgress<AudioChunk> audioProgress,
        CancellationToken ct = default
    );

    Task StopAsync(CancellationToken ct = default);
}
