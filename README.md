# soundboard-maui

Cross-platform .NET MAUI desktop client for the Sound Board voice engine.

> **This repository consumes the Sound Board public API only.**
> It does not embed or modify the engine.

## Status

MVP in development.

## Architecture

```
soundboard-maui (this repo)          voice-soundboard (engine repo)
┌──────────────────────┐             ┌──────────────────────┐
│  .NET MAUI Desktop   │  WebSocket  │  Python TTS Engine   │
│  Client Application  │ ──────────> │  (Kokoro ONNX)       │
│                      │             │                      │
│  - Text input        │             │  - 54+ voices        │
│  - Preset selection  │             │  - 19 emotions       │
│  - Playback control  │             │  - Streaming output  │
└──────────────────────┘             └──────────────────────┘
```

The MAUI client connects to the voice engine over WebSocket.
The engine runs as a separate process on the same machine.

## Engine Repository

[mcp-tool-shop-org/voice-soundboard](https://github.com/mcp-tool-shop-org/voice-soundboard)

## Requirements

- .NET 9.0+
- Windows 10/11 (primary target)
- Voice Soundboard engine running locally
