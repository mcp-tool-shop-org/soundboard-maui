namespace Soundboard.Client.Models;

public sealed record AudioChunk(
    byte[] PcmData,
    int SampleRate
);
