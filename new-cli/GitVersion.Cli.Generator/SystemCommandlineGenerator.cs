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

            var commandHandlerTemplate = Template.Parse(SystemCommandlineContent.CommandImplContent);

            var commandHandlerSource = commandHandlerTemplate.Render(new
            {
                Model = commandInfo,
                Constants.GeneratedNamespaceName
            }, member => member.Name);

            context.AddSource($"{commandInfo.CommandTypeName}Impl.g.cs", string.Join("\n", commandHandlerSource));
        }

        var commandHandlersModuleTemplate = Template.Parse(SystemCommandlineContent.CommandsModuleContent);
        var commandHandlersModuleSource = commandHandlersModuleTemplate.Render(new
        {
            Model = commandInfos,
            Constants.GeneratedNamespaceName,
            Constants.CommonNamespaceName,
            Constants.InfrastructureNamespaceName,
            Constants.CommandNamespaceName
        }, member => member.Name);
        context.AddSource("CommandsModule.g.cs", string.Join("\n", commandHandlersModuleSource));

        var rootCommandHandlerTemplate = Template.Parse(SystemCommandlineContent.RootCommandImplContent);
        var rootCommandHandlerSource = rootCommandHandlerTemplate.Render(new
        {
            Constants.GeneratedNamespaceName,
            Constants.CommonNamespaceName
        }, member => member.Name);
        context.AddSource("RootCommandImpl.g.cs", string.Join("\n", rootCommandHandlerSource));

        var cliAppTemplate = Template.Parse(SystemCommandlineContent.CliAppContent);
        var cliAppSource = cliAppTemplate.Render(new
        {
            Constants.GeneratedNamespaceName,
            Constants.InfrastructureNamespaceName,
            Constants.CommonNamespaceName
        }, member => member.Name);
        context.AddSource("CliAppImpl.g.cs", string.Join("\n", cliAppSource));
    }
}
