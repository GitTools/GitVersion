using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;

namespace GitVersion.App.Tests;

public class HelpWriterTests : TestBase
{
    private readonly IHelpWriter helpWriter;

    public HelpWriterTests()
    {
        var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule(useLegacyParser: true)));
        this.helpWriter = sp.GetRequiredService<IHelpWriter>();
    }

    [Test]
    public void AllArgsAreInHelp()
    {
        var lookup = new Dictionary<string, string>
        {
            { nameof(Arguments.IsHelp), "--help" },
            { nameof(Arguments.IsVersion), "--version" },

            { nameof(Arguments.TargetUrl), "--url" },
            { nameof(Arguments.TargetBranch), "--branch" },
            { nameof(Arguments.ClonePath), "--dynamic-repo-location" },
            { nameof(Arguments.CommitId), "--commit" },

            { nameof(Arguments.Diag), "--diagnose" },
            { nameof(Arguments.LogFilePath), "--log-file" },
            { "verbosity", "--verbosity" },
            { nameof(Arguments.Output), "--output" },
            { nameof(Arguments.OutputFile), "--output-file" },
            { nameof(Arguments.ShowVariable), "--show-variable" },
            { nameof(Arguments.Format), "--format" },

            { nameof(Arguments.UpdateWixVersionFile), "--update-wix-version-file" },
            { nameof(Arguments.UpdateProjectFiles), "--update-project-files" },
            { nameof(Arguments.UpdateAssemblyInfo), "--update-assembly-info" },
            { nameof(Arguments.EnsureAssemblyInfo), "--ensure-assembly-info" },

            { nameof(Arguments.ConfigurationFile), "--config" },
            { nameof(Arguments.ShowConfiguration), "--show-config" },
            { nameof(Arguments.OverrideConfiguration), "--override-config" },

            { nameof(Arguments.NoCache), "--no-cache" },
            { nameof(Arguments.NoFetch), "--no-fetch" },
            { nameof(Arguments.NoNormalize), "--no-normalize" },
            { nameof(Arguments.AllowShallow), "--allow-shallow" }
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

        // Fallback: convert PascalCase to kebab-case and check for --option
        var kebab = System.Text.RegularExpressions.Regex.Replace(propertyName, "(?<!^)([A-Z])", "-$1").ToLower();
        return !helpText.Contains("--" + kebab);
    }
}
