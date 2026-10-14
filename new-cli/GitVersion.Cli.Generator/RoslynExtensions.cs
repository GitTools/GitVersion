namespace GitVersion;

public static class RoslynExtensions
{
    extension(ITypeSymbol type) // NOSONAR: C# 14 extension block syntax; these are not overloads named extension.
    {
        private IEnumerable<ITypeSymbol> GetBaseTypesAndThis() // NOSONAR: Instance extension member using the type receiver.
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public IEnumerable<ITypeSymbol> GetBaseTypes() // NOSONAR: Instance extension member using the type receiver.
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public IEnumerable<T> GetAllMembers<T>() where T : ISymbol // NOSONAR: Instance extension member using the type receiver.
            => type.GetBaseTypesAndThis().SelectMany(n => n.GetMembers().OfType<T>());
    }

    extension(ISymbol namedType) // NOSONAR: C# 14 extension block syntax, not an identifier named extension.
    {
        public AttributeData? GetAttributeData(string fullName) // NOSONAR: Instance extension member using the namedType receiver.
            => namedType.GetAttributes()
                .SingleOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == fullName);
    }
}
