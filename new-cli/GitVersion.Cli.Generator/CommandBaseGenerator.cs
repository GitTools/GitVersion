using GitVersion.Polyfill;

namespace GitVersion;

public abstract class CommandBaseGenerator : IIncrementalGenerator
{
    private const string CommandInterfaceFullName = $"{Content.InfraNamespaceName}.ICommand<T>";
    private const string CommandAttributeFullName = $"{Content.InfraNamespaceName}.CommandAttribute";
    private const string CommandAttributeGenericFullName = $"{Content.InfraNamespaceName}.CommandAttribute<T>";
    private const string OptionAttributeFullName = $"{Content.InfraNamespaceName}.OptionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var commandTypes = context.CompilationProvider.Select(SelectCommandTypes);

        context.RegisterImplementationSourceOutput(commandTypes, GenerateSourceCode);
    }

    internal abstract void GenerateSourceCode(SourceProductionContext context, ImmutableArray<CommandInfo?> commandInfos);

    private static ImmutableArray<CommandInfo?> SelectCommandTypes(Compilation compilation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var visitor = new TypeVisitor(SearchQuery, ct);
        visitor.Visit(compilation.GlobalNamespace);
        var selectCommandTypes = visitor.GetResults();

        return [.. selectCommandTypes.Select(selectCommandType => MapToCommandInfo(selectCommandType, ct))];

        static bool SearchQuery(INamedTypeSymbol typeSymbol)
        {
            var attributeData = typeSymbol.GetAttributeData(CommandAttributeFullName) ?? typeSymbol.GetAttributeData(CommandAttributeGenericFullName);
            return attributeData is not null;
        }
    }

    private static CommandInfo? MapToCommandInfo(ITypeSymbol classSymbol, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var commandAttribute = classSymbol.GetAttributeData(CommandAttributeFullName) ?? classSymbol.GetAttributeData(CommandAttributeGenericFullName);
        if (commandAttribute is null) return null;

        var ctorArguments = commandAttribute.ConstructorArguments;

        var name = Convert.ToString(ctorArguments[0].Value);
        var description = Convert.ToString(ctorArguments[1].Value);

        name.NotNull();
        description.NotNull();

        ITypeSymbol? parentCommandType = null;
        if (commandAttribute.AttributeClass?.TypeArguments.Any() == true)
        {
            parentCommandType = commandAttribute.AttributeClass.TypeArguments.Single();
        }

        var commandBase = classSymbol.AllInterfaces.SingleOrDefault(x => x.OriginalDefinition.ToDisplayString() == CommandInterfaceFullName);
        if (commandBase is null) return null;

        var settingsType = commandBase.TypeArguments.Single();

        var properties = settingsType.GetAllMembers<IPropertySymbol>().ToArray();

        var settingsPropertyInfos =
            from propertySymbol in properties
            let optionAttribute = propertySymbol.GetAttributeData(OptionAttributeFullName)
            where optionAttribute is not null
            select MapToPropertyInfo(propertySymbol, optionAttribute);

        var commandInfo = new CommandInfo
        {
            ParentCommand = parentCommandType?.Name,
            CommandTypeName = classSymbol.Name,
            CommandTypeNamespace = classSymbol.ContainingNamespace.ToDisplayString(),
            CommandName = name,
            CommandDescription = description,
            SettingsTypeName = settingsType.Name,
            SettingsTypeNamespace = settingsType.ContainingNamespace.ToDisplayString(),
            SettingsProperties = [.. settingsPropertyInfos]
        };
        return commandInfo;
    }
    private static SettingsPropertyInfo MapToPropertyInfo(IPropertySymbol propertySymbol, AttributeData attribute)
    {
        var ctorArguments = attribute.ConstructorArguments;

        var name = Convert.ToString(ctorArguments[0].Value);
        var description = Convert.ToString(ctorArguments[1].Value);

        name.NotNull();
        description.NotNull();

        var alias = string.Empty;
        if (ctorArguments.Length == 3)
        {
            var aliasesArgs = ctorArguments[2];
            var aliases = (aliasesArgs.Kind == TypedConstantKind.Array
                ? aliasesArgs.Values.Select(x => Convert.ToString(x.Value)).ToArray()
                : [Convert.ToString(aliasesArgs.Value)]).Select(x => $@"""{x?.Trim()}""");
            alias = string.Join(", ", aliases);
        }

        var isRequired = propertySymbol.IsRequired;
        return new()
        {
            Name = propertySymbol.Name,
            TypeName = propertySymbol.Type.ToDisplayString(),
            OptionName = name,
            Aliases = alias,
            Description = description,
            Required = isRequired
        };
    }
}
