namespace Soundboard.Client.Models;

/// <summary>Engine health and version information returned by <c>GET /api/health</c>.</summary>
/// <param name="Status">Engine readiness status (e.g. "ready", "loading").</param>
/// <param name="EngineVersion">Semver version of the engine.</param>
/// <param name="ApiVersion">API contract version implemented by the engine.</param>
public sealed record EngineInfo(
    string Status,
    string EngineVersion,
    string ApiVersion
);
