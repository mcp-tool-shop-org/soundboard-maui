namespace Soundboard.Client.Models;

/// <summary>A request to synthesize speech via the engine.</summary>
/// <param name="Text">The text to speak.</param>
/// <param name="Preset">The preset identifier (e.g. "narrator").</param>
/// <param name="Voice">The voice identifier (e.g. "af_bella").</param>
/// <param name="RequestId">Optional correlation ID. Generated automatically if null.</param>
public sealed record SpeakRequest(
    string Text,
    string Preset,
    string Voice,
    string? RequestId = null
)
{
    /// <summary>Resolved ID: uses explicit value or generates one.</summary>
    internal string ResolvedRequestId => RequestId ?? Guid.NewGuid().ToString();
}
