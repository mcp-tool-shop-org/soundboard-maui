namespace Soundboard.Client.Tests;

public class DisposeTests
{
    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        var client = new SoundboardClient("http://localhost:9999");

        // Should not throw even if never used
        await client.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_DoubleDispose_DoesNotThrow()
    {
        var client = new SoundboardClient("http://localhost:9999");

        await client.DisposeAsync();
        await client.DisposeAsync();
    }
}
