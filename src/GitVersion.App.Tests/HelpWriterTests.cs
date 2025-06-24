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
            { nameof(Arguments.IsHelp), "/?" },
            { nameof(Arguments.IsVersion), "/version" },

            { nameof(Arguments.TargetUrl), "/url" },
            { nameof(Arguments.TargetBranch), "/b" },
            { nameof(Arguments.ClonePath), "/dynamicRepoLocation" },
            { nameof(Arguments.CommitId), "/c" },

            { nameof(Arguments.LogFilePath) , "/l" },
            { nameof(Arguments.Verbosity), "/verbosity" },
            { nameof(Arguments.Output) , "/output" },
            { nameof(Arguments.OutputFile) , "/outputfile" },
            { nameof(Arguments.ShowVariable), "/showvariable" },
            { nameof(Arguments.Format), "/format" },

            { nameof(Arguments.UpdateWixVersionFile), "/updatewixversionfile" },
            { nameof(Arguments.UpdateProjectFiles), "/updateprojectfiles" },
            { nameof(Arguments.UpdateAssemblyInfo), "/updateassemblyinfo" },
            { nameof(Arguments.EnsureAssemblyInfo), "/ensureassemblyinfo" },

            { nameof(Arguments.ConfigurationFile), "/config" },
            { nameof(Arguments.ShowConfiguration), "/showconfig" },
            { nameof(Arguments.OverrideConfiguration), "/overrideconfig" },

            { nameof(Arguments.NoCache), "/nocache" },
            { nameof(Arguments.NoFetch), "/nofetch" },
            { nameof(Arguments.NoNormalize), "/nonormalize" },
            { nameof(Arguments.AllowShallow), "/allowshallow" }
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

    private static bool IsNotInHelp(Dictionary<string, string> lookup, string propertyName, string helpText)
    {
        if (lookup.TryGetValue(propertyName, out var value))
            return !helpText.Contains(value);

        return !helpText.Contains("/" + propertyName.ToLower());
    }
}
