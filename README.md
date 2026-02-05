# Soundboard MAUI

A cross-platform desktop client for streaming voice engines, designed to demonstrate low-latency, interruptible TTS over a clean public API.

> **This repository is the client only.** It does not embed or modify the engine.

## What it does

Type text. Pick a style. Press Speak. Audio streams in real time.

Soundboard MAUI connects to a voice engine over WebSocket and streams PCM16 audio directly to your speakers. The first output should feel expressive and intentional, not robotic.

## What it is not

- Not a production TTS service
- Not an engine or model
- Not a research tool
- Not tied to any specific voice engine (any engine implementing the [API contract](docs/api-contract.md) works)

## Architecture

```
soundboard-maui (this repo)          voice-soundboard (engine repo)
+----------------------+             +----------------------+
|  .NET MAUI Desktop   |  WebSocket  |  Python TTS Engine   |
|  Client Application  | ----------> |  (Kokoro ONNX)       |
|                      |             |                      |
|  - Text input        |             |  - 54+ voices        |
|  - Preset selection  |             |  - 19 emotions       |
|  - Playback control  |             |  - Streaming output  |
+----------------------+             +----------------------+
```

The client and engine are separate processes on the same machine. The client knows nothing about models, weights, or inference. It speaks HTTP + WebSocket.

## Project structure

```
src/
  Soundboard.Client/       Pure C# service client (net8.0, no MAUI deps)
  Soundboard.Maui.Audio/   NAudio PCM16 playback adapter (Windows)
  Soundboard.Maui/         MAUI desktop app (net10.0-windows)

tests/
  Soundboard.Client.Tests/       27 unit tests (no engine required)
  Soundboard.IntegrationTests/   5 integration tests (FakeEngineServer)

docs/
  api.md                   Client API v1.0 stability guarantees
  api-contract.md          Engine <-> MAUI protocol spec
  feature-map.md           Feature-to-layer mapping
  integration-checklist.md Runbook for engine integration
  known-limitations.md     Current boundaries and failure modes
  evaluation-guide.md      How to evaluate Soundboard
```

## Quick start

```bash
# Clone
git clone https://github.com/mcp-tool-shop-org/soundboard-maui.git
cd soundboard-maui

# Run tests (no engine needed)
dotnet test

# Build the app
dotnet build src/Soundboard.Maui

# Point at your engine (default: http://localhost:8765)
set SOUNDBOARD_BASE_URL=http://localhost:8765
dotnet run --project src/Soundboard.Maui
```

## Requirements

- .NET 10.0 SDK (MAUI workload installed)
- Windows 10/11
- Voice engine running locally (see [engine repo](https://github.com/mcp-tool-shop-org/voice-soundboard))

## FAQ

**Is this production-ready?**
No. This is a demo-grade MVP proving the architecture. The client API (v1.0) is stable, but the app is not hardened for production use.

**Where does the engine live?**
In a separate repository: [mcp-tool-shop-org/voice-soundboard](https://github.com/mcp-tool-shop-org/voice-soundboard). This repo contains zero engine code.

**Can I build my own UI?**
Yes. `Soundboard.Client` is a standalone net8.0 library with no MAUI dependencies. Reference it from any .NET project and implement `IAudioPlayer` for your platform.

**What voices/presets are available?**
That depends entirely on the engine. The client discovers available presets and voices at runtime via the API.

**Why .NET MAUI?**
Windows-first desktop target with a path to cross-platform. The client library is platform-agnostic.

## Related

- [Engine repository](https://github.com/mcp-tool-shop-org/voice-soundboard)
- [API contract](docs/api-contract.md)
- [Evaluation guide](docs/evaluation-guide.md)
- [Contributing](CONTRIBUTING.md)
