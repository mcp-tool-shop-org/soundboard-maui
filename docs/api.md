# Soundboard.Client API v1.0

**Status:** Stable
**Version:** 1.0.0

## Guaranteed (Public API)

These are stable and will not change without a major version bump:

### Interface
- `ISoundboardClient : IAsyncDisposable`
  - `GetHealthAsync(CancellationToken)`
  - `GetPresetsAsync(CancellationToken)`
  - `GetVoicesAsync(CancellationToken)`
  - `SpeakAsync(SpeakRequest, IProgress<AudioChunk>, CancellationToken)`
  - `StopAsync(CancellationToken)`

### Models (`Soundboard.Client.Models`)
- `EngineInfo(Status, EngineVersion, ApiVersion)`
- `SpeakRequest(Text, Preset, Voice, RequestId?)`
- `AudioChunk(PcmData, SampleRate)`
- `EngineEvent(State)`

### Configuration
- `SoundboardClientOptions` record with `BaseUrl`, `HttpTimeout`, `WebSocketConnectTimeout`, `WebSocketReceiveTimeout`
- `SOUNDBOARD_BASE_URL` environment variable

### Audio Adapter
- `IAudioPlayer` interface: `Start`, `Feed`, `Stop`, `Flush`, `IsPlaying`, `BufferedChunks`

## Not Guaranteed (Internal)

These may change without a major version bump:

- Internal logging message formats and levels
- Default timeout values
- Transport implementation details (buffer sizes, JSON serialization options)
- Internal constructors (marked `internal`)
- `SpeakRequest.ResolvedRequestId` (internal helper)

## Breaking Change Policy

- Requires major version bump (1.x → 2.0)
- Must be documented in CHANGELOG
- Deprecated APIs get `[Obsolete]` for at least one minor release before removal
- No breaking changes in patch releases
