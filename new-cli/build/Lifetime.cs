using System;
using Build;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

public class Lifetime : FrostingLifetime<Context>
{
    public override void Setup(Context context)
    {
        context.Target = context.Argument("target", "Default");
        context.Configuration = context.Argument("configuration", "Release");
        context.FormatCode = context.Argument("formatCode", false);

        context.Artifacts = "./packaging/";
        context.CodeCoverage = "./coverage-results/";

        // Build system information.
        var buildSystem = context.BuildSystem();
        context.IsLocalBuild = buildSystem.IsLocalBuild;

        context.GitHubActions = buildSystem.GitHubActions.IsRunningOnGitHubActions;
        context.AzurePipelines = buildSystem.AzurePipelines.IsRunningOnAzurePipelines ||
                                 buildSystem.AzurePipelines.IsRunningOnAzurePipelinesHosted;

        context.IsTagged = context.IsBuildTagged();
        context.IsPullRequest = buildSystem.IsPullRequest;

        if (context.AzurePipelines)
        {
            context.RepositoryName = buildSystem.AzurePipelines.Environment.Repository.RepoName;
            context.BranchName = buildSystem.AzurePipelines.Environment.Repository.SourceBranchName;
        }
        else if (context.GitHubActions)
        {
            context.RepositoryName = buildSystem.GitHubActions.Environment.Workflow.Repository;
            context.BranchName = buildSystem.GitHubActions.Environment.Workflow.Ref.Replace("refs/heads/", "");
        }

        context.IsOriginalRepo = StringComparer.OrdinalIgnoreCase.Equals("gittools/gitversion", context.RepositoryName);
        context.IsMainBranch = StringComparer.OrdinalIgnoreCase.Equals("master", context.BranchName);

        // Force publish?
        context.ForcePublish = context.Argument("forcepublish", false);

        // Setup projects.
        context.Projects = new[]
        {
            new Context.Project { Name = "Octokit", Path = "./Octokit/Octokit.csproj", Publish = true },
            new Context.Project { Name = "Octokit.Reactive", Path = "./Octokit.Reactive/Octokit.Reactive.csproj", Publish = true },
            new Context.Project { Name = "Octokit.Tests", Path = "./Octokit.Tests/Octokit.Tests.csproj", UnitTests = true },
            new Context.Project { Name = "Octokit.Tests.Conventions", Path = "./Octokit.Tests.Conventions/Octokit.Tests.Conventions.csproj", ConventionTests = true },
            new Context.Project { Name = "Octokit.Tests.Integration", Path = "./Octokit.Tests.Integration/Octokit.Tests.Integration.csproj", IntegrationTests = true }
        };

        /*context.GitVersionToolPath = ToolInstaller.DotNetCoreToolInstall(context, "GitVersion.Tool", "5.1.3", "dotnet-gitversion");

        // Calculate semantic version.
        context.Version = BuildVersion.Calculate(context);
        context.Version.Prefix = context.Argument<string>("version", context.Version.Prefix);
        context.Version.Suffix = context.Argument<string>("suffix", context.Version.Suffix);*/

        /*context.Information("Version:        {0}", context.Version.Prefix);
        context.Information("Version suffix: {0}", context.Version.Suffix);*/
        context.Information("Configuration:  {0}", context.Configuration);
        context.Information("Target:         {0}", context.Target);
        context.Information("AzurePipelines: {0}", context.AzurePipelines);
        context.Information("Repository:     {0}", context.RepositoryName);
        context.Information("Branch:         {0}", context.BranchName);
        context.Information("GitHub Actions: {0}", context.GitHubActions);
    }

    public override void Teardown(Context context, ITeardownContext info)
    {
    }
}