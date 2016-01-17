using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GitVersion;
using LibGit2Sharp;
using Shouldly;

public abstract class RepositoryFixtureBase : IDisposable
{
    Dictionary<string, string> participants = new Dictionary<string, string>();
    Config configuration;
    StringBuilder diagramBuilder;

    protected RepositoryFixtureBase(Func<string, IRepository> repoBuilder, Config configuration)
        : this(configuration, repoBuilder(PathHelper.GetTempPath()))
    {
    }

    protected RepositoryFixtureBase(Config configuration, IRepository repository)
    {
        ConfigurationProvider.ApplyDefaultsTo(configuration);
        diagramBuilder = new StringBuilder();
        diagramBuilder.AppendLine("@startuml");
        this.configuration = configuration;
        Repository = repository;
        Repository.Config.Set("user.name", "Test");
        Repository.Config.Set("user.email", "test@email.com");
        IsForTrackedBranchOnly = true;
    }

    public bool IsForTrackedBranchOnly { private get; set; }
    public IRepository Repository { get; private set; }
    public string RepositoryPath { get { return Repository.Info.WorkingDirectory.TrimEnd('\\'); } }

    public void Checkout(string branch)
    {
        Repository.Checkout(branch);
    }

    public void Activate(string branch)
    {
        diagramBuilder.AppendLineFormat("activate {0}", GetParticipant(branch));
    }

    public void Destroy(string branch)
    {
        diagramBuilder.AppendLineFormat("destroy {0}", GetParticipant(branch));
    }

    public void Participant(string participant, string @as = null)
    {
        participants.Add(participant, @as ?? participant);
        if (@as == null)
            diagramBuilder.AppendLineFormat("participant {0}", participant);
        else
            diagramBuilder.AppendLineFormat("participant \"{0}\" as {1}", participant, @as);
    }

    public void Divider(string text)
    {
        diagramBuilder.AppendLineFormat("== {0} ==", text);
    }

    public void NoteOver(string noteText, string startNode, string endNode = null, string prefix = null)
    {
        diagramBuilder.AppendLineFormat(
prefix + @"note over {0}{1}
  {2}
end note",
GetParticipant(startNode),
endNode == null ? null : ", " + GetParticipant(endNode),
noteText.Replace("\n", "\n  "));
    }

    public void MakeATaggedCommit(string tag)
    {
        MakeACommit();
        ApplyTag(tag);
    }

    public void ApplyTag(string tag)
    {
        diagramBuilder.AppendLineFormat("{0} -> {0}: tag {1}", GetParticipant(Repository.Head.Name), tag);
        Repository.ApplyTag(tag);
    }

    public void BranchTo(string branchName, string @as = null)
    {
        if (!participants.ContainsKey(branchName))
        {
            diagramBuilder.Append("create ");
            Participant(branchName, @as);
        }

        var branch = Repository.Head.Name;
        diagramBuilder.AppendLineFormat("{0} -> {1}: branch from {2}", GetParticipant(branch), GetParticipant(branchName), branch);
        Repository.Checkout(Repository.CreateBranch(branchName));
    }

    public void BranchToFromTag(string branchName, string fromTag, string onBranch, string @as = null)
    {
        if (!participants.ContainsKey(branchName))
        {
            diagramBuilder.Append("create ");
            Participant(branchName, @as);
        }

        diagramBuilder.AppendLineFormat("{0} -> {1}: branch from tag ({2})", GetParticipant(onBranch), GetParticipant(branchName), fromTag);
        Repository.Checkout(Repository.CreateBranch(branchName));
    }

    public void MakeACommit()
    {
        diagramBuilder.AppendLineFormat("{0} -> {0}: commit", GetParticipant(Repository.Head.Name));
        Repository.MakeACommit();
    }

    public void MergeNoFF(string mergeTarget)
    {
        diagramBuilder.AppendLineFormat("{0} -> {1}: merge", GetParticipant(mergeTarget), GetParticipant(Repository.Head.Name));
        Repository.MergeNoFF(mergeTarget, Constants.SignatureNow());
    }

    public void AssertFullSemver(string fullSemver, IRepository repository = null, string commitId = null)
    {
        Console.WriteLine("---------");

        try
        {
            var variables = GetVersion(repository, commitId);
            variables.FullSemVer.ShouldBe(fullSemver);
            (repository ?? Repository).DumpGraph();
        }
        catch (Exception)
        {
            (repository ?? Repository).DumpGraph();
            throw;
        }
        if (commitId == null)
            diagramBuilder.AppendLineFormat("note over {0} #D3D3D3: {1}", GetParticipant(Repository.Head.Name), fullSemver);
    }

    string GetParticipant(string branch)
    {
        if (participants.ContainsKey(branch))
            return participants[branch];

        return branch;
    }

    public VersionVariables GetVersion(IRepository repository = null, string commitId = null)
    {
        var gitVersionContext = new GitVersionContext(repository ?? Repository, configuration, IsForTrackedBranchOnly, commitId);
        var executeGitVersion = ExecuteGitVersion(gitVersionContext);
        var variables = VariableProvider.GetVariablesFor(executeGitVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
        try
        {
            return variables;
        }
        catch (Exception)
        {
            Console.WriteLine("Test failing, dumping repository graph");
            gitVersionContext.Repository.DumpGraph();
            throw;
        }
    }

    SemanticVersion ExecuteGitVersion(GitVersionContext context)
    {
        var vf = new GitVersionFinder();
        return vf.FindVersion(context);
    }

    public virtual void Dispose()
    {
        Repository.Dispose();

        try
        {
            DirectoryHelper.DeleteDirectory(RepositoryPath);
        }
        catch (Exception e)
        {
            Trace.WriteLine(string.Format("Failed to clean up repository path at {0}. Received exception: {1}", RepositoryPath, e.Message));
        }

        diagramBuilder.AppendLine("@enduml");
        Trace.WriteLine("**Visualisation of test:**");
        Trace.WriteLine(string.Empty);
        Trace.WriteLine(diagramBuilder.ToString());
    }

    public LocalRepositoryFixture CloneRepository(Config config = null)
    {
        var localPath = PathHelper.GetTempPath();
        LibGit2Sharp.Repository.Clone(RepositoryPath, localPath);
        return new LocalRepositoryFixture(config ?? new Config(), new Repository(localPath));
    }
}