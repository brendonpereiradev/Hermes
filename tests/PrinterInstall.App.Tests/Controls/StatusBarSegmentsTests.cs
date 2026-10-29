using PrinterInstall.App.Controls;

namespace PrinterInstall.App.Tests.Controls;

public class StatusBarSegmentsTests
{
    [Fact]
    public void Compute_splits_counts_into_fractions_of_total()
    {
        var segments = StatusBarSegments.Compute(succeeded: 2, failed: 1, total: 4);

        Assert.Equal(0.5, segments.Success);
        Assert.Equal(0.25, segments.Failure);
        Assert.Equal(0.25, segments.Pending);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Compute_without_targets_returns_empty_bar(int total)
    {
        Assert.Equal(default, StatusBarSegments.Compute(succeeded: 1, failed: 1, total));
    }

    [Fact]
    public void Compute_with_all_finished_leaves_nothing_pending()
    {
        var segments = StatusBarSegments.Compute(succeeded: 3, failed: 2, total: 5);

        Assert.Equal(0, segments.Pending);
    }

    [Fact]
    public void Compute_ignores_negative_counts()
    {
        var segments = StatusBarSegments.Compute(succeeded: -1, failed: -2, total: 4);

        Assert.Equal(0, segments.Success);
        Assert.Equal(0, segments.Failure);
        Assert.Equal(1, segments.Pending);
    }

    [Theory]
    [InlineData(5, 0, 4)]
    [InlineData(3, 3, 4)]
    [InlineData(0, 9, 4)]
    public void Compute_clamps_counts_so_the_bar_never_overflows(int succeeded, int failed, int total)
    {
        var segments = StatusBarSegments.Compute(succeeded, failed, total);

        Assert.InRange(segments.Success + segments.Failure, 0, 1);
        Assert.Equal(1, segments.Success + segments.Failure + segments.Pending, precision: 10);
    }
}
