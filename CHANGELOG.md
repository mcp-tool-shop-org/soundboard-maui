# Changelog

All notable changes to the Soundboard SDK and reference clients.

Format follows [Keep a Changelog](https://keepachangelog.com/). SDK versions follow [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-02-05

### SDK (Soundboard.Client)
- Initial stable release
- `ISoundboardClient` interface with health, presets, voices, speak, stop
- Streaming audio via `IProgress<AudioChunk>` over WebSocket
- `CancellationToken` on every async method
- `SoundboardClientOptions` with environment variable support (`SOUNDBOARD_BASE_URL`)
- Runtime API version check with warning on mismatch (`SdkApiVersion` constant)
- XML documentation on all public types
- NuGet package metadata

### Reference clients
- MAUI desktop app with welcome flow, offline resilience, tap-to-reconnect
- CLI client (`soundboard-cli`) with health, presets, voices, speak commands

### Examples
- Quickstart: zero-config connect → speak → WAV
- AgentTool: SDK as callable tool in an agent pipeline

### Documentation
- API reference (v1.0 stability guarantees)
- API contract (engine ↔ SDK wire protocol)
- Compatibility & trust contract
- Getting started guide
- Streaming model
- Error model
- Engine distribution guide
- Upgrade/rollback playbooks

### Tests
- 27 unit tests
- 17 integration + contract tests
