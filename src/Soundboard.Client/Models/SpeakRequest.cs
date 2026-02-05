namespace Soundboard.Client.Models;

public sealed record SpeakRequest(
    string Text,
    string Preset,
    string Voice
);
