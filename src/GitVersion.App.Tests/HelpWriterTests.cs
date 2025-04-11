using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.App.Tests;

public class HelpWriterTests : TestBase
{
    private readonly IHelpWriter helpWriter;

    public HelpWriterTests()
    {
        var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule()));
        this.helpWriter = sp.GetRequiredService<IHelpWriter>();
    }

    [Test]
    public void AllArgsAreInHelp()
    {
        var lookup = new Dictionary<string, string>
        {
            { nameof(Arguments.TargetUrl), "/url" },
            { nameof(Arguments.TargetBranch), "/b" },
            { nameof(Arguments.LogFilePath) , "/l" },
            { nameof(Arguments.OutputFile) , "/outputfile" },
            { nameof(Arguments.ClonePath), "/dynamicRepoLocation" },
            { nameof(Arguments.IsHelp), "/?" },
            { nameof(Arguments.IsVersion), "/version" },
            { nameof(Arguments.UpdateWixVersionFile), "/updatewixversionfile" },
            { nameof(Arguments.ConfigurationFile), "/config" },
            { nameof(Arguments.Verbosity), "/verbosity" },
            { nameof(Arguments.CommitId), "/c" },
            { nameof(Arguments.ShowConfiguration), "/showconfig" },
            { nameof(Arguments.OverrideConfiguration), "/overrideconfig" },
            { nameof(Arguments.ShowVariable), "/showvariable" },
            { nameof(Arguments.Format), "/format" }
        };
        var helpText = string.Empty;

        this.helpWriter.WriteTo(s => helpText = s);

        var ignored = new[]
        {
            nameof(Arguments.Authentication),
            nameof(Arguments.UpdateAssemblyInfoFileName)
        };
        typeof(Arguments).GetFields()
            .Select(p => p.Name)
            .Where(p => IsNotInHelp(lookup, p, helpText))
            .Except(ignored)
            .ShouldBeEmpty();
    }

    private static bool IsNotInHelp(IReadOnlyDictionary<string, string> lookup, string propertyName, string helpText)
    {
        if (lookup.TryGetValue(propertyName, out var value))
            return !helpText.Contains(value);

        return !helpText.Contains("/" + propertyName.ToLower());
    }
}
