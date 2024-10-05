namespace GitVersion;

public static class RoslynExtensions
{
    private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static IEnumerable<T> GetAllMembers<T>(this ITypeSymbol type) where T : ISymbol
        => type.GetBaseTypesAndThis().SelectMany(n => n.GetMembers().OfType<T>());

    public static AttributeData? GetAttributeData(this ISymbol namedType, string fullName)
        => namedType.GetAttributes()
            .SingleOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == fullName);
}
