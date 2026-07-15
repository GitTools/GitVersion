using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;
using LibGit2Sharp;

namespace GitVersion.Tests;

[TestFixture]
public class GitCliMutatorTests : TestBase
{
    private static GitCliMutator CreateMutator() =>
        new(NullLogger<GitCliMutator>.Instance, new GitCliExecutor(NullLogger<GitCliExecutor>.Instance));

    [Test]
    public void CheckoutSwitchesToTheGivenBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("feature/cli");

        CreateMutator().Checkout(fixture.RepositoryPath, "feature/cli");

        fixture.Repository.Head.FriendlyName.ShouldBe("feature/cli");
    }

    [Test]
    public void CheckoutOfUnknownSpecThrows()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        Should.Throw<InvalidOperationException>(() => CreateMutator().Checkout(fixture.RepositoryPath, "does-not-exist"));
    }

    [Test]
    public void CheckoutFailureIsNotMisclassifiedAsAnHttpError()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        // The failing spec echoes "404" in stderr; only network operations may map
        // HTTP status codes, so the real pathspec error must surface unchanged.
        var exception = Should.Throw<InvalidOperationException>(
            () => CreateMutator().Checkout(fixture.RepositoryPath, "fix-404-page"));

        exception.Message.ShouldContain("fix-404-page");
        exception.Message.ShouldNotContain("The repository was not found");
    }

    [Test]
    public void FetchBringsNewRemoteCommitsIntoTheLocalRepository()
    {
        using var fixture = new RemoteRepositoryFixture();
        var newCommit = fixture.Repository.MakeACommit();
        var localRepository = fixture.LocalRepositoryFixture.Repository;
        localRepository.Lookup(newCommit.Sha).ShouldBeNull();

        CreateMutator().Fetch(fixture.LocalRepositoryFixture.RepositoryPath, "origin", [], new AuthenticationInfo());

        localRepository.Lookup(newCommit.Sha).ShouldNotBeNull();
    }

    [Test]
    public void CloneCreatesRepositoryWithoutCheckingOutTheWorkingDirectory()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        var targetPath = FileSystemHelper.Path.GetRepositoryTempPath();

        try
        {
            CreateMutator().Clone(fixture.RepositoryPath, targetPath, new AuthenticationInfo());

            using var clonedRepository = new Repository(targetPath);
            clonedRepository.Head.Tip.ShouldNotBeNull();
            Directory.EnumerateFileSystemEntries(targetPath)
                .Where(entry => FileSystemHelper.Path.GetFileName(entry) != ".git")
                .ShouldBeEmpty();
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(targetPath);
        }
    }

    [Test]
    public void CloneOfMissingRepositoryThrows()
    {
        var missingSource = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPathLegacy(), "gitversion-does-not-exist");
        var targetPath = FileSystemHelper.Path.GetRepositoryTempPath();

        Should.Throw<InvalidOperationException>(() => CreateMutator().Clone(missingSource, targetPath, new AuthenticationInfo()));
    }

    [Test]
    public void CloneOfMissingHttpRepositoryMapsGitsNotFoundPhrasing()
    {
        // Non-GitHub hosts produce "fatal: repository '<url>' not found" (no "404",
        // no contiguous "Repository not found"); the message contract must still hold.
        using var server = new HttpNotFoundServer();
        var targetPath = FileSystemHelper.Path.GetRepositoryTempPath();

        var exception = Should.Throw<InvalidOperationException>(
            () => CreateMutator().Clone($"{server.Url}/missing/repo.git", targetPath, new AuthenticationInfo()));

        exception.Message.ShouldBe("Not found: The repository was not found");
    }

    private sealed class HttpNotFoundServer : IDisposable
    {
        private readonly System.Net.HttpListener listener = new();
        public string Url { get; }

        public HttpNotFoundServer()
        {
            var port = GetFreePort();
            Url = $"http://127.0.0.1:{port}";
            this.listener.Prefixes.Add($"{Url}/");
            this.listener.Start();
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                while (this.listener.IsListening)
                {
                    try
                    {
                        var context = await this.listener.GetContextAsync().ConfigureAwait(false);
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                    catch (Exception ex) when (ex is System.Net.HttpListenerException or ObjectDisposedException)
                    {
                        return;
                    }
                }
            });
        }

        private static int GetFreePort()
        {
            using var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            socket.Start();
            return ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
        }

        public void Dispose() => this.listener.Stop();
    }

    [Test]
    public void ListRemoteReferencesReturnsBranchTips()
    {
        using var fixture = new RemoteRepositoryFixture();
        var remoteTipSha = fixture.Repository.Head.Tip.Sha;

        var references = CreateMutator().ListRemoteReferences(fixture.LocalRepositoryFixture.RepositoryPath, "origin", new AuthenticationInfo());

        references.Single(r => r.CanonicalName == "refs/heads/main").TargetSha.ShouldBe(remoteTipSha);
        references.ShouldNotContain(r => r.CanonicalName == "HEAD");
    }
}
