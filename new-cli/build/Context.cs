using System;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

public class Context : FrostingContext
{
    public string? Target { get; set; }
    public new string? Configuration { get; set; }
    public bool FormatCode { get; set; }
    // public BuildVersion Version { get; set; }

    public DirectoryPath? Artifacts { get; set; }
    public DirectoryPath? Packages { get; set; }
    public DirectoryPath? CodeCoverage { get; set; }

    public bool IsLocalBuild { get; set; }
    public bool IsPullRequest { get; set; }
    public bool IsOriginalRepo { get; set; }
    public bool IsTagged { get; set; }
    public bool IsMainBranch { get; set; }
    public bool ForcePublish { get; set; }

    public string? RepositoryName { get; set; }
    public string? BranchName { get; set; }

    public bool AzurePipelines { get; set; }
    public bool GitHubActions { get; set; }

    public Project[] Projects { get; set; } = Array.Empty<Project>();

    public Context(ICakeContext context)
        : base(context)
    {
    }

    public class Project
    {
        public string? Name { get; set; }
        public FilePath? Path { get; set; }
        public bool Publish { get; set; }
        public bool UnitTests { get; set; }
        public bool ConventionTests { get; set; }
        public bool IntegrationTests { get; set; }
        public bool IsTests => UnitTests || ConventionTests || IntegrationTests;
    }
}