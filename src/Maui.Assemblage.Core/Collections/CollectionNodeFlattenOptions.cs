namespace Maui.Assemblage.Core.Collections;

public sealed record CollectionNodeFlattenOptions
{
    public object? Header { get; init; }
    public object? Footer { get; init; }
    public object? EmptyView { get; init; }
    public bool IncludeSectionHeaders { get; init; } = true;
    public bool IncludeSectionFooters { get; init; } = true;
}
