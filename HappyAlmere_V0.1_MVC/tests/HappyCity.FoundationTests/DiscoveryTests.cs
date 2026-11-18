using HappyCity.Application;
using HappyCity.Infrastructure;

namespace HappyCity.FoundationTests;

public sealed class DiscoveryTests
{
    [Theory]
    [InlineData(null, null, 6)]
    [InlineData("activiteiten", null, 2)]
    [InlineData(null, " FAMILIE ", 1)]
    [InlineData(" ETEN ", "lunch", 1)]
    [InlineData("diensten", "familie", 0)]
    [InlineData(null, "does-not-exist", 0)]
    [InlineData(null, "   ", 6)]
    [InlineData("unknown", null, 0)]
    public async Task Search_has_predictable_filtering(string? category, string? query, int count)
    {
        var service = new DiscoveryService(new DemoDiscoveryRepository());
        var results = await service.FindAsync(new DiscoveryFilter(category, query));
        Assert.Equal(count, results.Count);
    }

    [Fact]
    public void Search_limits_are_enforced_at_application_boundary()
    {
        Assert.Throws<ArgumentException>(() => new DiscoveryFilter(null, new string('x', 101)));
        Assert.Throws<ArgumentException>(() => new DiscoveryFilter(new string('x', 31), null));
        var filter = new DiscoveryFilter(new string('x', 30), new string('x', 100));
        Assert.Equal(100, filter.Query!.Length);
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        var service = new DiscoveryService(new DemoDiscoveryRepository());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.FindAsync(new DiscoveryFilter(null, null), cancellation.Token));
    }
}
