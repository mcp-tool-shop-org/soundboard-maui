namespace Soundboard.Client.Models;

public sealed record EngineInfo(
    string Status,
    string EngineVersion,
    string ApiVersion
);
