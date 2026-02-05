using Soundboard.Client.Models;

namespace Soundboard.Client.Tests;

public class HealthTests
{
    [Fact]
    public async Task GetHealthAsync_DeserializesCorrectly()
    {
        var handler = new MockHttpHandler(_ => MockHttpHandler.Json("""
            {
                "status": "ok",
                "engineVersion": "0.9.0",
                "apiVersion": "1"
            }
            """));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8765") };
        var client = new SoundboardClient(http, new Uri("ws://localhost:8765/stream"));

        var health = await client.GetHealthAsync();

        Assert.Equal("ok", health.Status);
        Assert.Equal("0.9.0", health.EngineVersion);
        Assert.Equal("1", health.ApiVersion);
    }

    [Fact]
    public async Task GetHealthAsync_ThrowsOnNull()
    {
        var handler = new MockHttpHandler(_ => MockHttpHandler.Json("null"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8765") };
        var client = new SoundboardClient(http, new Uri("ws://localhost:8765/stream"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetHealthAsync());
    }
}
