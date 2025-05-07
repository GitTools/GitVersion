namespace GitVersion;

[Generator(LanguageNames.CSharp)]
public class SystemCommandlineGenerator : CommandBaseGenerator
{
    private const string GeneratedNamespaceName = "GitVersion.Generated";
    private const string InfraNamespaceName = "GitVersion";
    private const string DependencyInjectionNamespaceName = "GitVersion.Infrastructure";
    private const string CommandNamespaceName = "GitVersion.Commands";

    internal override void GenerateSourceCode(SourceProductionContext context, ImmutableArray<CommandInfo?> commandInfos)
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
}
