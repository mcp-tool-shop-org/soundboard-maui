using System.Net;

namespace Soundboard.Client.Tests;

public class StopTests
{
    [Fact]
    public async Task StopAsync_SendsPostRequest()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8765") };
        var client = new SoundboardClient(http, new Uri("ws://localhost:8765/stream"));

        await client.StopAsync();

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("/api/stop", captured.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task StopAsync_RespectsCanellation()
    {
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8765") };
        var client = new SoundboardClient(http, new Uri("ws://localhost:8765/stream"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.StopAsync(cts.Token));
    }
}
