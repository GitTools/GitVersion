namespace GitVersion;

public static class SystemCommandlineContent
{
    /*language=cs*/
    public const string CommandImplContent = $$$"""
{{{Constants.GeneratedHeader}}}
using System.CommandLine;
using System.CommandLine.Binding;

using {{Model.CommandTypeNamespace}};

namespace {{GeneratedNamespaceName}};

public class {{Model.CommandTypeName}}Impl : Command, ICommandImpl
{
    public string CommandImplName => nameof({{Model.CommandTypeName}}Impl);
    {{- if (Model.ParentCommand | string.empty) }}
    public string ParentCommandImplName => string.Empty;
    {{- else }}
    public string ParentCommandImplName => nameof({{Model.ParentCommand}}Impl);
    {{ end }}
    {{- $settingsProperties = Model.SettingsProperties | array.sort "Name" }}
    // Options list
    {{~ for $prop in $settingsProperties ~}}
    protected readonly Option<{{$prop.TypeName}}> {{$prop.Name}}Option;
    {{~ end ~}}

    public {{Model.CommandTypeName}}Impl({{Model.CommandTypeName}} command)
        : base("{{Model.CommandName}}", "{{Model.CommandDescription}}")
    {
        {{~ for $prop in $settingsProperties ~}}
        {{$prop.Name}}Option = new Option<{{$prop.TypeName}}>("{{$prop.OptionName}}"{{if $prop.Aliases.size == 0}}{{else}}, {{for $alias in $prop.Aliases}}"{{$alias}}"{{if !for.last}}, {{end}}{{end}}{{end}})
        {
            Required = {{$prop.Required}},
            Description = "{{$prop.Description}}",
        };
        {{~ end ~}}

        {{- for $prop in $settingsProperties ~}}
        Add({{$prop.Name}}Option);
        {{~ end ~}}

        this.SetAction(Run);
        return;

        Task<int> Run(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var settings = new {{ if Model.SettingsTypeNamespace != Model.CommandTypeNamespace }}{{Model.SettingsTypeNamespace}}.{{ end }}{{Model.SettingsTypeName}}
            {
                {{~ for $prop in $settingsProperties ~}}
                {{$prop.Name}} = parseResult.GetValue({{$prop.Name}}Option){{ if $prop.Required }}!{{ end}},
                {{~ end ~}}
            };
            return command.InvokeAsync(settings, cancellationToken);
        }
    }
}
""";

    /*language=cs*/
    public const string RootCommandImplContent = $$$"""
{{{Constants.GeneratedHeader}}}
using System.CommandLine;

using {{CommonNamespaceName}};
using {{ExtensionsNamespaceName}};

namespace {{GeneratedNamespaceName}};

public class RootCommandImpl(IEnumerable<ICommandImpl> commands) : RootCommand
{
    private readonly IEnumerable<ICommandImpl> _commands = commands.NotNull();

    public void Configure()
    {
        var map = _commands.ToDictionary(c => c.CommandImplName);
        foreach (var command in map.Values)
        {
            AddCommand(command, map);
        }
    }

    private void AddCommand(ICommandImpl command, Dictionary<string, ICommandImpl> map)
    {
        if (!string.IsNullOrWhiteSpace(command.ParentCommandImplName))
        {
            var parent = map[command.ParentCommandImplName] as Command;
            parent?.Add((Command)command);
        }
        else
        {
            Add((Command)command);
        }
    }
}
""";

    /*language=cs*/
    public const string CommandsModuleContent = $$$"""
{{{Constants.GeneratedHeader}}}
using System.CommandLine;
using {{InfrastructureNamespaceName}};
using {{CommandNamespaceName}};
using {{CommonNamespaceName}};
using Microsoft.Extensions.DependencyInjection;

namespace {{GeneratedNamespaceName}};

public class CommandsModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<ICliApp, CliAppImpl>();
        services.AddSingleton<RootCommandImpl>();

        {{- $commands = Model | array.sort "CommandTypeName" }}

        {{~ for $command in $commands ~}}
        services.AddSingleton<{{ if $command.CommandTypeNamespace != CommandNamespaceName }}{{$command.CommandTypeNamespace}}.{{ end }}{{$command.CommandTypeName}}>();
        services.AddSingleton<ICommandImpl, {{$command.CommandTypeName}}Impl>();
        {{~ end ~}}
    }
}
""";

    /*language=cs*/
    public const string CliAppContent = $$$"""
{{{Constants.GeneratedHeader}}}
using System.CommandLine;
using {{ExtensionsNamespaceName}};
using {{InfrastructureNamespaceName}};

namespace {{GeneratedNamespaceName}};

internal class CliAppImpl : ICliApp
{
    private readonly RootCommandImpl _rootCommand;

    public CliAppImpl(RootCommandImpl rootCommand)
    {
        _rootCommand = rootCommand.NotNull();
        _rootCommand.Configure();
    }

    public Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        // Note: there are 2 locations to watch for the dotnet-suggest tool
        // - sentinel file:
        //  $env:TEMP\system-commandline-sentinel-files\ and
        // - registration file:
        //  $env:LOCALAPPDATA\.dotnet-suggest-registration.txt or $HOME/.dotnet-suggest-registration.txt

        var parseResult = _rootCommand.Parse(args);

        var logFile = parseResult.GetValue<FileInfo?>(GitVersionSettings.LogFileOption);
        var verbosity = parseResult.GetValue<Verbosity?>(GitVersionSettings.VerbosityOption) ?? Verbosity.Normal;

        LoggingEnricher.Configure(logFile?.FullName, verbosity);

        return parseResult.InvokeAsync(cancellationToken: cancellationToken);
    }
}
""";
}
