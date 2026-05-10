namespace Maui.Assemblage.Core.Collections;

public enum CollectionNodeKind
{
    Item,
    SectionHeader,
    SectionFooter,
    Header,
    Footer,
    Empty
}

public readonly record struct CollectionNode(
    CollectionNodeKind Kind,
    int Section,
    int Index,
    object? Data);
