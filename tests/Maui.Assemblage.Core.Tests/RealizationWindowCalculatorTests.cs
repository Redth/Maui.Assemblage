using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Realization;

namespace Maui.Assemblage.Core.Tests;

public class RealizationWindowCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsVisibleRangeWithCache()
    {
        var range = RealizationWindowCalculator.Calculate(
            offset: 70d,
            viewportSize: 100d,
            itemCount: 20,
            itemExtent: 30d,
            spacing: 10d,
            cacheBefore: 1,
            cacheAfter: 2);

        Assert.Equal(new ItemRange(0, 7), range);
    }

    [Fact]
    public void Calculate_ClampsRangeToItemCount()
    {
        var range = RealizationWindowCalculator.Calculate(
            offset: 900d,
            viewportSize: 180d,
            itemCount: 10,
            itemExtent: 100d,
            spacing: 0d,
            cacheBefore: 2,
            cacheAfter: 3);

        Assert.Equal(new ItemRange(7, 10), range);
    }
}
