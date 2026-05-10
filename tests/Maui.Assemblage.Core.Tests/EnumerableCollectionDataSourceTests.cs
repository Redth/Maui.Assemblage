using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Core.Tests;

public class EnumerableCollectionDataSourceTests
{
    [Fact]
    public void Getters_ReturnSingleSectionValues()
    {
        var source = new EnumerableCollectionDataSource(
            items: new object?[] { "a", "b", "c" },
            sectionHeader: "header",
            sectionFooter: "footer");

        Assert.Equal(1, source.SectionCount);
        Assert.Equal(3, source.GetItemCount(0));
        Assert.Equal("a", source.GetItem(0, 0));
        Assert.Equal("c", source.GetItem(0, 2));
        Assert.Equal("header", source.GetSectionHeader(0));
        Assert.Equal("footer", source.GetSectionFooter(0));
    }

    [Fact]
    public void InvalidSection_Throws()
    {
        var source = new EnumerableCollectionDataSource(items: new object?[] { 1 });

        Assert.Throws<ArgumentOutOfRangeException>(() => source.GetItemCount(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.GetSectionHeader(-1));
    }
}
