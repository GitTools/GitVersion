using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GitVersion;

internal class TypeVisitor : SymbolVisitor
{
    private readonly CancellationToken cancellationToken;
    private readonly HashSet<INamedTypeSymbol> exportedTypes;
    private readonly Func<INamedTypeSymbol, bool> searchQuery;

    public TypeVisitor(Func<INamedTypeSymbol, bool> searchQuery, CancellationToken cancellation)
    {
        cancellationToken = cancellation;
        exportedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        this.searchQuery = searchQuery;
    }

    public ImmutableArray<INamedTypeSymbol> GetResults() => exportedTypes.ToImmutableArray();

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        cancellationToken.ThrowIfCancellationRequested();
        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var namespaceOrType in symbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            namespaceOrType.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol type)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (searchQuery(type))
        {
            exportedTypes.Add(type);
        }
    }
}
