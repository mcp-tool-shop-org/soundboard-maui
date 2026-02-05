namespace Soundboard.Client.Tests;

public class VoicesTests
{
    [Fact]
    public async Task GetVoicesAsync_ParsesVoiceIds()
    {
        var handler = new MockHttpHandler(_ => MockHttpHandler.Json("""
            {
                "voices": [
                    { "id": "af_bella", "name": "Bella" },
                    { "id": "am_adam", "name": "Adam" }
                ]
            }
            """));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8765") };
        var client = new SoundboardClient(http, new Uri("ws://localhost:8765/stream"));

        var voices = await client.GetVoicesAsync();

        Assert.Equal(2, voices.Count);
        Assert.Equal("af_bella", voices[0]);
        Assert.Equal("am_adam", voices[1]);
    }

    [Fact]
    public async Task GetVoicesAsync_EmptyList()
    {
        var handler = new MockHttpHandler(_ => MockHttpHandler.Json("""
            {
                "voices": []
            }
            """));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8765") };
        var client = new SoundboardClient(http, new Uri("ws://localhost:8765/stream"));

        var voices = await client.GetVoicesAsync();

        Assert.Empty(voices);
    }
}
