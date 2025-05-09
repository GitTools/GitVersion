namespace GitVersion;

[Generator(LanguageNames.CSharp)]
public class SystemCommandlineGenerator : CommandBaseGenerator
{
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
                Content.GeneratedNamespaceName
            }, member => member.Name);

            context.AddSource($"{commandInfo.CommandTypeName}Impl.g.cs", string.Join("\n", commandHandlerSource));
        }

        var commandHandlersModuleTemplate = Template.Parse(Content.CommandsModuleContent);
        var commandHandlersModuleSource = commandHandlersModuleTemplate.Render(new
        {
            Model = commandInfos,
            Content.GeneratedNamespaceName,
            Content.CommonNamespaceName,
            Content.InfrastructureNamespaceName,
            Content.CommandNamespaceName
        }, member => member.Name);
        context.AddSource("CommandsModule.g.cs", string.Join("\n", commandHandlersModuleSource));

        var rootCommandHandlerTemplate = Template.Parse(Content.RootCommandImplContent);
        var rootCommandHandlerSource = rootCommandHandlerTemplate.Render(new
        {
            Content.GeneratedNamespaceName,
            Content.CommonNamespaceName
        }, member => member.Name);
        context.AddSource("RootCommandImpl.g.cs", string.Join("\n", rootCommandHandlerSource));
    }
}
