using System.CommandLine;
using GitVersion.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace GitVersion.Cli.Generator.Tests;

public class SystemCommandlineGeneratorTests
{
    /*language=cs*/
    private const string TestCommandSourceCode =
"""
using System.Threading;
using System.Threading.Tasks;
using GitVersion.Infrastructure;

namespace GitVersion.Commands;

public record TestCommandSettings
{
    [Option("--output-file", "The output file")]
    public required string OutputFile { get; init; }
}

[CommandAttribute("test", "Test description.")]
public class TestCommand(ILogger logger): ICommand<TestCommandSettings>
{
    public Task<int> InvokeAsync(TestCommandSettings settings, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

""";

    /*language=cs*/
    private const string ExpectedCommandImplText =
$$"""
{{Content.GeneratedHeader}}
using System.CommandLine;
using System.CommandLine.Binding;
using System.Threading;
using System.Threading.Tasks;

using GitVersion.Commands;

namespace GitVersion.Generated;

public class TestCommandImpl : Command, ICommandImpl
{
    public string CommandName => nameof(TestCommandImpl);
    public string ParentCommandName => string.Empty;
    // Options list
    protected readonly Option<string> OutputFileOption;

    public TestCommandImpl(TestCommand command)
        : base("test", "Test description.")
    {
        OutputFileOption = new Option<string>("--output-file", [])
        {
            Required = false,
            Description = "The output file",
        };
        Add(OutputFileOption);

        this.SetAction(Run);
        return;

        Task<int> Run(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var settings = new TestCommandSettings
            {
                OutputFile = parseResult.GetValue(OutputFileOption),
            };
            return command.InvokeAsync(settings, cancellationToken);
        }
    }
}
""";

    /*language=cs*/
    private const string ExpectedCommandsModuleText =
$$"""
{{Content.GeneratedHeader}}
using System.CommandLine;
using GitVersion.Infrastructure;
using GitVersion.Commands;
using GitVersion;

namespace GitVersion.Generated;

public class CommandsModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<RootCommandImpl>();
        services.AddSingleton<TestCommand>();
        services.AddSingleton<ICommandImpl, TestCommandImpl>();
    }
}
""";

    /*language=cs*/
    private const string ExpectedRootCommandImplText =
$$"""
{{Content.GeneratedHeader}}
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

using GitVersion;
namespace GitVersion.Generated;

public class RootCommandImpl : RootCommand
{
    public RootCommandImpl(IEnumerable<ICommandImpl> commands)
    {
        var map = commands.ToDictionary(c => c.CommandName);
        foreach (var command in map.Values)
        {
            AddCommand(command, map);
        }
    }
    private void AddCommand(ICommandImpl command, IDictionary<string, ICommandImpl> map)
    {
        if (!string.IsNullOrWhiteSpace(command.ParentCommandName))
        {
            var parent = map[command.ParentCommandName] as Command;
            parent?.Add((Command)command);
        }
        else
        {
            Add((Command)command);
        }
    }
}
""";

    [Test]
    public async Task ValidateGeneratedCommandImplementation()
    {
        var generatorType = typeof(SystemCommandlineGenerator);
        var sourceGeneratorTest = new CSharpSourceGeneratorTest<SystemCommandlineGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    (generatorType, "TestCommand.cs", TestCommandSourceCode)
                },
                GeneratedSources =
                {
                    (generatorType,"TestCommandImpl.g.cs", ExpectedCommandImplText),
                    (generatorType,"CommandsModule.g.cs", ExpectedCommandsModuleText),
                    (generatorType,"RootCommandImpl.g.cs", ExpectedRootCommandImplText),
                },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(ILogger).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(RootCommand).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(CommandAttribute).Assembly.Location),
                }
            }
        };

        await sourceGeneratorTest.RunAsync();
    }
}
