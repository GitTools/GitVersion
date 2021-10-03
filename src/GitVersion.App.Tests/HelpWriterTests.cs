using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.App.Tests;

public class HelpWriterTests : TestBase
{
    private readonly IHelpWriter helpWriter;

    public HelpWriterTests()
    {
        var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule()));
        this.helpWriter = sp.GetService<IHelpWriter>();
    }

    [Test]
    public void AllArgsAreInHelp()
    {
        var lookup = new Dictionary<string, string>
        {
            { nameof(Arguments.TargetUrl), "/url" },
            { nameof(Arguments.Init), "init" },
            { nameof(Arguments.TargetBranch), "/b" },
            { nameof(Arguments.LogFilePath) , "/l" },
            { nameof(Arguments.OutputFile) , "/outputfile" },
            { nameof(Arguments.DynamicRepositoryClonePath), "/dynamicRepoLocation" },
            { nameof(Arguments.IsHelp), "/?" },
            { nameof(Arguments.IsVersion), "/version" },
            { nameof(Arguments.UpdateWixVersionFile), "/updatewixversionfile" },
            { nameof(Arguments.ConfigFile), "/config" },
            { nameof(Arguments.Verbosity), "/verbosity" },
            { nameof(Arguments.CommitId), "/c" },
        };
        string helpText = null;

        this.helpWriter.WriteTo(s => helpText = s);

        var ignored = new[]
        {
            nameof(Arguments.Authentication),
        };
        typeof(Arguments).GetFields()
            .Select(p => p.Name)
            .Where(p => IsNotInHelp(lookup, p, helpText))
            .Except(ignored)
            .ShouldBeEmpty();
    }

    private static bool IsNotInHelp(IReadOnlyDictionary<string, string> lookup, string propertyName, string helpText)
    {
        if (lookup.ContainsKey(propertyName))
            return !helpText.Contains(lookup[propertyName]);

        return !helpText.Contains("/" + propertyName.ToLower());
    }
}
