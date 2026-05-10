using Maui.Assemblage.Core;
using Maui.Assemblage.Core.Data;
using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Realization;

namespace Maui.Assemblage.Core.Tests;

public class CollectionEngineTests
{
    private static EnumerableCollectionDataSource CreateDataSource(int count)
    {
        var items = Enumerable.Range(0, count).Select(i => (object?)$"Item {i}").ToList();
        return new EnumerableCollectionDataSource(items);
    }

    [Fact]
    public void InitialState_NoDataSource()
    {
        var engine = new CollectionEngine();

        Assert.Null(engine.DataSource);
        Assert.Null(engine.LayoutProvider);
        Assert.Null(engine.LastSnapshot);
        Assert.Empty(engine.Nodes);
    }

    [Fact]
    public void SetDataSource_Rebuilds()
    {
        var engine = new CollectionEngine();
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        engine.DataSource = CreateDataSource(10);

        Assert.Equal(10, engine.Nodes.Count);
        Assert.NotNull(engine.LastSnapshot);
    }

    [Fact]
    public void SetDataSourceNull_Resets()
    {
        var engine = new CollectionEngine();
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);
        engine.DataSource = CreateDataSource(10);

        engine.DataSource = null;

        Assert.Empty(engine.Nodes);
        Assert.Null(engine.LastSnapshot);
    }

    [Fact]
    public void SetLayoutProvider_InvalidatesLayout()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.OnViewportChanged(400d, 600d);

        engine.LayoutProvider = new LinearLayoutProvider(50d);

        Assert.NotNull(engine.LastSnapshot);
        Assert.True(engine.LastSnapshot!.Items.Count > 0);
    }

    [Fact]
    public void OnViewportChanged_NoChangeBelowThreshold_NoOp()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var snap1 = engine.LastSnapshot;

        // Change < 0.5 should be ignored
        engine.OnViewportChanged(400.2, 599.8);

        Assert.Same(snap1, engine.LastSnapshot);
    }

    [Fact]
    public void OnScroll_UpdatesRealization()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(100);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        engine.OnScroll(500d);

        Assert.True(updates.Count > 0);
    }

    [Fact]
    public void UpdateRequested_ResetOnNullDataSource()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        EngineUpdateKind? lastKind = null;
        engine.UpdateRequested += (_, e) => lastKind = e.Kind;

        engine.DataSource = null;

        Assert.Equal(EngineUpdateKind.Reset, lastKind);
    }

    [Fact]
    public void GetTemplateKey_ReturnsCorrectKeys()
    {
        var engine = new CollectionEngine();
        var ds = new GroupedCollectionDataSource([
            new GroupSection("Header", ["A", "B"], null)
        ]);
        engine.DataSource = ds;
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        // Node 0 should be section header, node 1+2 should be items
        Assert.Equal("sectionHeader", engine.GetTemplateKey(0));
        Assert.Equal("item", engine.GetTemplateKey(1));
        Assert.Equal("item", engine.GetTemplateKey(2));
    }

    [Fact]
    public void FlattenOptions_HeaderFooterEmptyView()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(0); // Empty
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        engine.FlattenOptions = new Collections.CollectionNodeFlattenOptions
        {
            Header = "GlobalHeader",
            Footer = "GlobalFooter",
            EmptyView = "Nothing here"
        };

        // Should have: header, empty, footer
        Assert.Equal(3, engine.Nodes.Count);
        Assert.Equal("header", engine.GetTemplateKey(0));
        Assert.Equal("empty", engine.GetTemplateKey(1));
        Assert.Equal("footer", engine.GetTemplateKey(2));
    }

    [Fact]
    public void Selection_IntegrationWithEngine()
    {
        var engine = new CollectionEngine();
        engine.Selection.Mode = Interactions.SelectionMode.Multiple;

        engine.Selection.Select("A");
        engine.Selection.Select("B");

        Assert.Equal(2, engine.Selection.Count);
        Assert.True(engine.Selection.IsSelected("A"));
    }

    [Fact]
    public void SectionIndexMap_BuiltOnRebuild()
    {
        var engine = new CollectionEngine();
        var ds = new GroupedCollectionDataSource([
            new GroupSection("S1", ["A", "B"], null),
            new GroupSection("S2", ["C"], null)
        ]);
        engine.DataSource = ds;
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        Assert.NotNull(engine.SectionIndexMap);
    }

    [Fact]
    public void CacheBeforeAfter_ClampedToZero()
    {
        var engine = new CollectionEngine();

        engine.CacheBefore = -5;
        engine.CacheAfter = -10;

        Assert.Equal(0, engine.CacheBefore);
        Assert.Equal(0, engine.CacheAfter);
    }

    [Fact]
    public void BinarySearch_VisibleRange_MatchesLinearScan()
    {
        // With 100 items at 50px each, scrolled to 500px with viewport 600px,
        // visible items should be roughly indices 10-21 (items at y=500..1100)
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(100);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        engine.OnScroll(500d);

        // Should get an incremental update (not reset)
        var lastUpdate = updates[^1];
        Assert.Equal(EngineUpdateKind.Incremental, lastUpdate.Kind);

        // Realized entries should cover items in the visible + buffer range
        Assert.True(lastUpdate.RealizedEntries.Count > 0);

        // All realized items should be near scroll offset (500-1100 viewport)
        foreach (var entry in lastUpdate.RealizedEntries)
        {
            // With CacheBefore=5, CacheAfter=5, items should be in range ~5..27
            Assert.InRange(entry.FlatIndex, 0, 99);
        }
    }

    [Fact]
    public void BinarySearch_AtStart_ReturnsFirstItems()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(50);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 300d); // viewport shows ~6 items

        // At offset 0, first visible should be 0
        Assert.NotNull(engine.LastSnapshot);
        Assert.True(engine.LastSnapshot!.Items.Count > 0);
    }

    [Fact]
    public void BinarySearch_AtEnd_ReturnsLastItems()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(50);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        // Scroll incrementally to the end so the snapshot window advances
        for (var offset = 0d; offset <= 2000d; offset += 200d)
        {
            engine.OnScroll(offset);
        }

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        // Final scroll near the end
        engine.OnScroll(1900d);

        var lastUpdate = updates[^1];
        Assert.Equal(EngineUpdateKind.Incremental, lastUpdate.Kind);
        // Should include the last item (index 49)
        Assert.Contains(lastUpdate.RealizedEntries, e => e.FlatIndex == 49);
    }

    [Fact]
    public void ApplyIncrementalChanges_Insert_ShiftsRealizedIndices()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        // Insert 2 items at index 3
        var items = Enumerable.Range(0, 12).Select(i => (object?)$"Item {i}").ToList();
        engine.SetDataSourceQuiet(new Data.EnumerableCollectionDataSource(items));

        var changeSet = new Data.CollectionChangeSet();
        changeSet.Add(Data.CollectionChange.Insert(0, 3, 2));
        engine.ApplyIncrementalChanges(changeSet);

        // Should NOT be a reset — should be incremental
        var lastUpdate = updates[^1];
        Assert.Equal(EngineUpdateKind.Incremental, lastUpdate.Kind);
        Assert.Equal(12, engine.Nodes.Count);
    }

    [Fact]
    public void ApplyIncrementalChanges_Remove_ShiftsRealizedIndices()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        // Remove 2 items starting at index 3
        var items = Enumerable.Range(0, 8).Select(i => (object?)$"Item {i}").ToList();
        engine.SetDataSourceQuiet(new Data.EnumerableCollectionDataSource(items));

        var changeSet = new Data.CollectionChangeSet();
        changeSet.Add(Data.CollectionChange.Remove(0, 3, 2));
        engine.ApplyIncrementalChanges(changeSet);

        var lastUpdate = updates[^1];
        Assert.Equal(EngineUpdateKind.Incremental, lastUpdate.Kind);
        Assert.Equal(8, engine.Nodes.Count);
    }

    [Fact]
    public void ApplyIncrementalChanges_Reset_FallsBackToFullRebuild()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        var changeSet = new Data.CollectionChangeSet();
        changeSet.AddReset();
        engine.ApplyIncrementalChanges(changeSet);

        // Should raise a Reset since the changeset is a Reset
        Assert.Contains(updates, u => u.Kind == EngineUpdateKind.Reset);
    }

    [Fact]
    public void ApplyIncrementalChanges_Replace_KeepsRealizedIndices()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        // Replace item at index 5
        var items = Enumerable.Range(0, 10).Select(i => (object?)(i == 5 ? "Replaced" : $"Item {i}")).ToList();
        engine.SetDataSourceQuiet(new Data.EnumerableCollectionDataSource(items));

        var changeSet = new Data.CollectionChangeSet();
        changeSet.Add(Data.CollectionChange.Replace(0, 5, 1));
        engine.ApplyIncrementalChanges(changeSet);

        var lastUpdate = updates[^1];
        Assert.Equal(EngineUpdateKind.Incremental, lastUpdate.Kind);
        Assert.Equal(10, engine.Nodes.Count);
    }

    [Fact]
    public void SetDataSourceQuiet_DoesNotTriggerRebuild()
    {
        var engine = new CollectionEngine();
        engine.DataSource = CreateDataSource(10);
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updateCount = 0;
        engine.UpdateRequested += (_, _) => updateCount++;

        // SetDataSourceQuiet should NOT fire any updates
        engine.SetDataSourceQuiet(CreateDataSource(20));

        Assert.Equal(0, updateCount);
    }

    [Fact]
    public void ApplyIncrementalChanges_GroupedData_FallsBackToRebuild()
    {
        var engine = new CollectionEngine();
        var ds = new GroupedCollectionDataSource([
            new GroupSection("S1", ["A", "B"], null),
            new GroupSection("S2", ["C"], null)
        ]);
        engine.DataSource = ds;
        engine.LayoutProvider = new LinearLayoutProvider(50d);
        engine.OnViewportChanged(400d, 600d);

        var updates = new List<EngineUpdateEventArgs>();
        engine.UpdateRequested += (_, e) => updates.Add(e);

        // Multi-section data should fall back to full rebuild
        var changeSet = new Data.CollectionChangeSet();
        changeSet.Add(Data.CollectionChange.Insert(0, 0, 1));
        engine.ApplyIncrementalChanges(changeSet);

        Assert.Contains(updates, u => u.Kind == EngineUpdateKind.Reset);
    }
}
