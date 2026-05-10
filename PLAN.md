# Ultimate MAUI Collection Controls Plan (.NET 10)

## 1) Vision and Scope

Build a high-performance, cross-platform collection controls engine for .NET MAUI/.NET 10 that can power:

- ListView-style vertical/horizontal lists
- GridView-style fixed/adaptive grids
- CarouselView-style paged/snap layouts
- CoverFlow/library-style transformed layouts
- Grouped/sectioned data with headers/footers

Core goal: keep almost all behavior in shared code (layout, virtualization, realization, templates, selection, grouping, swipe actions), while using native scroll containers only for gesture physics, momentum, and bounce.

## 2) Product Principles

1. **Shared-first architecture**: no UICollectionView/RecyclerView dependency for layout/virtualization.
2. **Composable engine**: one control core + pluggable layout providers and behaviors.
3. **Massive-data ready**: virtualization and recycling are mandatory from day one.
4. **Predictable API**: collection APIs consistent across list/grid/carousel/library variants.
5. **Feature parity baseline**: grouping, swipe actions, pull-to-refresh, empty views, template selectors, section/global headers and footers.

## 3) Key Lessons from Existing Patterns

### From UICollectionView-style architecture

- Keep strict separation between:
  - **Data source**
  - **Layout provider**
  - **Reusable view realization**
- Use **layout invalidation contexts** (re-layout only what changed).
- Maintain reusable cells/supplementary views in pools keyed by template/type.
- Support prefetch windows to avoid pop-in during fast scroll.

### From FunctionZero.Maui.Controls (ListViewZero)

- Shared-code virtualization works in MAUI.
- Template-keyed view caching/reuse is effective.
- Scroll offset synchronization needs guarding/coercion to avoid event storms.
- Very large content extents may require scroll-space scaling strategies on some platforms.

## 4) Proposed Architecture

## 4.1 Layered Design

### A. Public Control Layer

`CollectionHostView` (single control type) with convenience wrappers:

- `ListHostView` (preconfigured list layout)
- `GridHostView` (preconfigured grid layout)
- `CarouselHostView` (preconfigured paging layout)
- `LibraryHostView` (preconfigured transformed layout)

All wrappers forward into the same internal engine.

### B. Core Engine Layer (shared)

1. **Data Pipeline**
   - Adapts `IEnumerable`, `IList`, `INotifyCollectionChanged`, grouped sources, and optional custom data source API.
   - Produces a stable flattened stream of display nodes:
     - global header/footer
     - section header/footer
     - item
     - empty view

2. **Layout Pipeline**
   - `ILayoutProvider` computes item frames and transforms from viewport and data metadata.
   - Output is a `LayoutSnapshot` (content extent + attributes for realized indices).

3. **Viewport/Scroll Pipeline**
   - Native `ScrollView` host captures offsets, velocity, and scroll state.
   - Shared engine decides visible/cached ranges and layout invalidation.

4. **Realization Pipeline**
   - Recycle pools keyed by template key + view kind.
   - Binds/unbinds template roots.
   - Positions views in an absolute content surface.

5. **Interaction Pipeline**
   - Selection/focus, gestures, context swipe actions, drag hooks (future).

### C. Thin Platform Layer

- Platform scroll host adapters only:
  - read/write offset
  - observe scrolling state
  - expose bounce/physics toggles
  - refresh interaction bridge

No platform layout engine usage for item arrangement.

## 4.2 Core Interfaces

```csharp
public interface ICollectionDataSource
{
    int SectionCount { get; }
    int GetItemCount(int section);
    object? GetItem(int section, int index);
    object? GetSectionHeader(int section);
    object? GetSectionFooter(int section);
}

public interface ILayoutProvider
{
    LayoutCapabilities Capabilities { get; }
    LayoutSnapshot Arrange(LayoutContext context, Range requestRange);
    InvalidationPlan Invalidate(LayoutInvalidationContext context);
}

public interface IRealizationStrategy
{
    void Realize(RealizationContext context, LayoutSnapshot snapshot);
    void RecycleOutside(Range keepRange);
}
```

## 4.3 Layout Provider Model

Ship these providers first:

1. `LinearLayoutProvider` (vertical/horizontal)
2. `GridLayoutProvider` (fixed span + adaptive min-item-size)
3. `CarouselLayoutProvider` (paging, snap points, peek)
4. `CoverFlowLayoutProvider` (transform/scale/alpha based on center distance)

Each provider returns:

- frame (`Rect`)
- z-index
- transform matrix
- opacity
- sticky metadata (for pinned headers)

## 4.4 Realization + Recycling Strategy

- **Windowing**: realize visible range plus cache-before/cache-after.
- **Pool buckets**: `(ViewKind, TemplateKey)` -> stack/queue of reusable views.
- **Bind lifecycle**:
  - `PrepareForReuse`
  - set binding context
  - apply layout attributes
- **Measurement cache**:
  - key by `(TemplateKey, constrained width, data shape hash)`
  - support fixed-size and dynamic-size items

## 4.5 Data and Change Tracking

- Adapter listens to collection/source changes and converts to normalized change sets.
- Use diffing for reset/replace operations to minimize relayout/realization churn.
- Keep stable identities via optional `ItemKeySelector`.
- Grouping represented as sections; flattening map maintained for fast index translation.

## 4.6 Feature Surfaces

Must support in v1 architecture:

- Item template + item template selector
- Section header/footer template selectors
- Global header/footer templates
- Empty view template
- Single/multi selection
- Pull to refresh
- Swipe context actions
- Sticky headers (list/grid sections)
- Programmatic scroll APIs (`ScrollToItem`, `ScrollToOffset`)

## 5) Performance Strategy

1. **Frame budget target**: 60 fps baseline, 120 fps best-effort on capable devices.
2. **Main-thread discipline**:
   - offload diffing and heavy layout math where safe
   - marshal only final UI operations to main thread
3. **Incremental invalidation**:
   - bounds-only updates do not trigger full data rebind
   - data mutation invalidates only impacted sections/ranges
4. **Prefetching**:
   - realize ahead based on velocity and direction
5. **Memory controls**:
   - bounded pools with eviction policy
   - optional low-memory compaction hook
6. **Large scroll extents**:
   - include scroll-space scaling strategy for platform max-range quirks

## 6) API Shape (Initial)

```csharp
public class CollectionHostView : TemplatedView
{
    public object? ItemsSource { get; set; }
    public ICollectionDataSource? DataSource { get; set; }
    public ILayoutProvider LayoutProvider { get; set; }

    public DataTemplate? ItemTemplate { get; set; }
    public DataTemplateSelector? ItemTemplateSelector { get; set; }

    public DataTemplate? SectionHeaderTemplate { get; set; }
    public DataTemplate? SectionFooterTemplate { get; set; }
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? FooterTemplate { get; set; }
    public DataTemplate? EmptyViewTemplate { get; set; }

    public SelectionMode SelectionMode { get; set; }
    public IReadOnlyList<object> SelectedItems { get; }
}
```

## 7) Phased Implementation Plan

## Phase 0 - Foundations (2-3 weeks)

- Project scaffolding (`src`, `samples`, `benchmarks`, `tests`)
- Control shell and platform scroll adapters
- Core node model (item/header/footer/section/empty)
- Basic realization and recycling pool
- Benchmark harness and trace hooks

**Exit criteria**: fixed-height vertical list with recycling and smooth scroll.

## Phase 1 - List + Grouping + Core Features (3-4 weeks)

- `LinearLayoutProvider`
- Grouped sections + section headers/footers + global header/footer
- Selection modes and events
- Empty view
- Pull-to-refresh integration
- Programmatic scroll APIs

**Exit criteria**: production-grade grouped list scenario with 100k items test.

## Phase 2 - Grid + Dynamic Measurement (3-4 weeks)

- `GridLayoutProvider` (fixed/adaptive)
- Dynamic item measurement + cache
- Sticky section headers for vertical grids
- Better invalidation contexts

**Exit criteria**: adaptive grid with variable heights and stable fps.

## Phase 3 - Carousel + Library/CoverFlow (3 weeks)

- `CarouselLayoutProvider` with snapping/paging
- `CoverFlowLayoutProvider` transforms
- Center-item tracking, snap heuristics, velocity-aware targeting

**Exit criteria**: smooth paging and transformed layout demos.

## Phase 4 - Context Actions + Polish (3 weeks)

- Swipe-to-reveal actions
- Interaction arbitration (scroll vs swipe)
- Accessibility polish (screen reader order, semantics, actions)
- RTL and keyboard/focus traversal

**Exit criteria**: feature-complete v1 candidate.

## Phase 5 - Hardening + Release (2-3 weeks)

- Performance and memory stress testing
- API review and naming cleanup
- Samples/documentation
- NuGet packaging and versioning

**Exit criteria**: v1 release package with docs and sample gallery.

## 8) Testing and Validation

1. **Unit tests**
   - range calculations
   - layout provider math
   - diff/change-set generation
2. **Integration tests**
   - collection changes under scroll
   - selection consistency during recycling
   - grouped + template selector correctness
3. **Performance tests**
   - cold/warm scroll traces
   - allocation rate and pool hit ratio
   - stress tests at 10k/100k/1m logical items
4. **Device matrix**
   - iOS, Android, Mac Catalyst, Windows

## 9) Risks and Mitigations

- **Risk**: dynamic height layouts can cause relayout storms.  
  **Mitigation**: estimated sizes + deferred precise measurement + section-scoped invalidation.

- **Risk**: gesture conflicts (swipe actions vs scroll).  
  **Mitigation**: explicit gesture state machine with directional lock thresholds.

- **Risk**: API complexity explosion.  
  **Mitigation**: one core control + typed preset wrappers + progressive advanced APIs.

- **Risk**: platform-specific scroll quirks.  
  **Mitigation**: isolate in adapter layer and add cross-platform scroll behavior tests.

## 10) Recommended Starting Point (First Build Slice)

Start with: non-grouped vertical list, fixed item height, single template, recycle pool, and scroll-window realization.  
Then add grouped nodes and template selectors before tackling dynamic heights and advanced layouts.  
This sequence de-risks the hardest parts while preserving architecture for all target layout types.
