namespace GitVersion;

public static class RoslynExtensions
{
    extension(ITypeSymbol type)
    {
        private IEnumerable<ITypeSymbol> GetBaseTypesAndThis()
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public IEnumerable<ITypeSymbol> GetBaseTypes()
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public IEnumerable<T> GetAllMembers<T>() where T : ISymbol
            => type.GetBaseTypesAndThis().SelectMany(n => n.GetMembers().OfType<T>());
    }

    extension(ISymbol namedType)
    {
        public AttributeData? GetAttributeData(string fullName)
            => namedType.GetAttributes()
                .SingleOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == fullName);
    }
}
