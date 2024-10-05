namespace GitVersion;

internal class TypeVisitor(Func<INamedTypeSymbol, bool> searchQuery, CancellationToken cancellation)
    : SymbolVisitor
{
    private readonly HashSet<INamedTypeSymbol> _exportedTypes = new(SymbolEqualityComparer.Default);

    public ImmutableArray<INamedTypeSymbol> GetResults() => [.. this._exportedTypes];

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        cancellation.ThrowIfCancellationRequested();
        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var namespaceOrType in symbol.GetMembers())
        {
            cancellation.ThrowIfCancellationRequested();
            namespaceOrType.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol type)
    {
        cancellation.ThrowIfCancellationRequested();

        if (searchQuery(type))
        {
            this._exportedTypes.Add(type);
        }
    }
}
