using GitVersion.Polyfill;

// ReSharper disable InconsistentNaming
namespace GitVersion;

[Generator(LanguageNames.CSharp)]
public class CommandImplGenerator : IIncrementalGenerator
{
    private const string GeneratedNamespaceName = "GitVersion.Generated";
    private const string InfraNamespaceName = "GitVersion";
    private const string DependencyInjectionNamespaceName = "GitVersion.Infrastructure";
    private const string CommandNamespaceName = "GitVersion.Commands";
    private const string CommandInterfaceFullName = $"{InfraNamespaceName}.ICommand<T>";
    private const string CommandAttributeFullName = $"{InfraNamespaceName}.CommandAttribute";
    private const string CommandAttributeGenericFullName = $"{InfraNamespaceName}.CommandAttribute<T>";
    private const string OptionAttributeFullName = $"{InfraNamespaceName}.OptionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var commandTypes = context.CompilationProvider.Select(SelectCommandTypes);

        context.RegisterImplementationSourceOutput(commandTypes, GenerateSourceCode);
    }

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
    private static void GenerateSourceCode(SourceProductionContext context, ImmutableArray<CommandInfo?> commandInfos)
    {
        foreach (var commandInfo in commandInfos)
        {
            if (commandInfo == null)
                continue;

            var commandHandlerTemplate = Template.Parse(Content.CommandImplContent);

            var commandHandlerSource = commandHandlerTemplate.Render(new
            {
                Model = commandInfo,
                Namespace = GeneratedNamespaceName
            }, member => member.Name);

            context.AddSource($"{commandInfo.CommandTypeName}Impl.g.cs", string.Join("\n", commandHandlerSource));
        }

        var commandHandlersModuleTemplate = Template.Parse(Content.CommandsModuleContent);
        var commandHandlersModuleSource = commandHandlersModuleTemplate.Render(new
        {
            Model = commandInfos,
            Namespace = GeneratedNamespaceName,
            InfraNamespaceName,
            DependencyInjectionNamespaceName,
            CommandNamespaceName
        }, member => member.Name);
        context.AddSource("CommandsModule.g.cs", string.Join("\n", commandHandlersModuleSource));

        var rootCommandHandlerTemplate = Template.Parse(Content.RootCommandImplContent);
        var rootCommandHandlerSource = rootCommandHandlerTemplate.Render(new
        {
            Namespace = GeneratedNamespaceName,
            InfraNamespaceName
        }, member => member.Name);
        context.AddSource("RootCommandImpl.g.cs", string.Join("\n", rootCommandHandlerSource));
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
            SettingsProperties = settingsPropertyInfos.ToArray()
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

        string alias = string.Empty;
        if (ctorArguments.Length == 3)
        {
            var aliasesArgs = ctorArguments[2];
            var aliases = (aliasesArgs.Kind == TypedConstantKind.Array
                ? aliasesArgs.Values.Select(x => Convert.ToString(x.Value)).ToArray()
                : [Convert.ToString(aliasesArgs.Value)]).Select(x => $@"""{x?.Trim()}""");
            alias = string.Join(", ", aliases);
        }

        var isRequired = propertySymbol.Type.NullableAnnotation == NullableAnnotation.NotAnnotated;
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
