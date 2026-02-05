namespace Soundboard.Client.Models;

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
