using GitVersion.Telemetry;

namespace GitVersion;

/// <summary>Projects CLI arguments into telemetry eligibility flags and allowlisted option names.</summary>
internal static class TelemetryInvocationFactory
{
    /// <summary>Keeps raw values in the CLI and defers option projection until an event is selected.</summary>
    internal static TelemetryInvocation Create(Arguments args) => new(args.TelemetryOptOut,
        args.IsHelp || args.IsVersion || args.IsCliSchema || args.ValidateConfiguration ||
        args.IsConfigurationMigration || args.ShowConfiguration || args.ConfigurationPropertyPath != null,
        () => CanonicalOptions(args),
        () => typeof(Arguments).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>Extracts canonical option names and allowlisted output modes while discarding free-form values.</summary>
    private static string[] CanonicalOptions(Arguments args)
    {
        var flags = new List<string>();
        Add(args.NoCache, "--no-cache");
        Add(args.NoFetch, "--no-fetch");
        Add(args.NoNormalize, "--no-normalize");
        Add(args.AllowShallow, "--allow-shallow");
        Add(args.Diag, "--diagnose");
        Add(args.ConfigurationFile != null, "--config");
        Add(args.OverrideConfiguration.Count > 0, "--override-config");
        Add(args.ShowVariable != null, "--show-variable");
        Add(args.Format != null, "--format");
        Add(args.TargetUrl != null, "--url");
        Add(args.TargetBranch != null, "--branch");
        Add(args.CommitId != null, "--commit");
        Add(args.ClonePath != null, "--dynamic-repo-location");
        Add(args.LogFilePath != null, "--log-file");
        Add(args.OutputFile != null, "--output-file");
        Add(args.UpdateAssemblyInfo, "--update-assembly-info");
        Add(args.UpdateProjectFiles, "--update-project-files");
        Add(args.EnsureAssemblyInfo, "--ensure-assembly-info");
        Add(args.UpdateWixVersionFile, "--update-wix-version-file");
        foreach (var output in args.Output.Order())
        {
            var name = output switch
            {
                OutputType.Json => "json",
                OutputType.BuildServer => "buildserver",
                OutputType.File => "file",
                OutputType.DotEnv => "dotenv",
                _ => null
            };
            if (name != null)
            {
                flags.Add("--output=" + name);
            }
        }
        return [.. flags];

        void Add(bool present, string name)
        {
            if (present)
            {
                flags.Add(name);
            }
        }
    }
}
