using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;
using GitVersion.Testing.Git;

namespace GitVersion.Tests;

[TestFixture]
public class GitCliMutatorTests : TestBase
{
    private static GitCliMutator CreateMutator() =>
        new(NullLogger<GitCliMutator>.Instance, new GitCliExecutor(NullLogger<GitCliExecutor>.Instance));

    [Test]
    public void ExecutorCapturesCompleteAsynchronousOutput()
    {
        using var fixture = new EmptyRepositoryFixture();
        var lines = Enumerable.Range(0, 20_000).Select(index => $"line {index:D5}").ToList();
        var contents = string.Join(SysEnv.NewLine, lines) + SysEnv.NewLine;
        var path = FileSystemHelper.Path.Combine(fixture.RepositoryPath, "large-output.txt");
        FileSystemHelper.File.WriteAllText(path, contents);
        Commands.Stage(fixture.Repository, path);
        fixture.Repository.Commit("large output", Generate.SignatureNow(), Generate.SignatureNow());

        var result = new GitCliExecutor(NullLogger<GitCliExecutor>.Instance)
            .Execute(fixture.RepositoryPath, ["show", "HEAD:large-output.txt"]);

        result.IsSuccess.ShouldBeTrue();
        result.StandardOutput.ShouldBe(contents);
        result.StandardError.ShouldBeEmpty();
    }

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
    [NonParallelizable]
    public void CheckoutIgnoresAnInheritedGitDirEnvironmentVariable()
    {
        // git exports GIT_DIR when running hooks; the executor must scrub it so
        // mutations target the working-directory repository, as libgit2 did.
        using var otherFixture = new EmptyRepositoryFixture();
        otherFixture.Repository.MakeACommit();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("only-here");

        System.Environment.SetEnvironmentVariable("GIT_DIR", FileSystemHelper.Path.Combine(otherFixture.RepositoryPath, ".git"));
        try
        {
            CreateMutator().Checkout(fixture.RepositoryPath, "only-here");
        }
        finally
        {
            System.Environment.SetEnvironmentVariable("GIT_DIR", null);
        }

        fixture.Repository.Head.FriendlyName.ShouldBe("only-here");
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

    [TestCase(40123)]
    [TestCase(40323)]
    [TestCase(40423)]
    public void CloneOfMissingHttpRepositoryIgnoresStatusCodeDigitsInUrl(int port)
    {
        var message = $"fatal: repository 'http://127.0.0.1:{port}/missing/repo.git/' not found";

        var exception = Should.Throw<InvalidOperationException>(
            () => CreateFailingMutator(message).Clone("https://example.test/repo.git", "unused", new AuthenticationInfo()));

        exception.Message.ShouldBe("Not found: The repository was not found");
    }

    [TestCase(401)]
    [TestCase(403)]
    [TestCase(404)]
    public void CloneFailureDoesNotClassifyStatusCodeDigitsInUrlsOrPaths(int code)
    {
        var message = $"Cloning into '/tmp/repo-{code}'...\nfatal: unable to access 'https://example.test/repo/{code}/': Could not resolve host: example.test";

        var exception = Should.Throw<InvalidOperationException>(
            () => CreateFailingMutator(message).Clone("https://example.test/repo.git", "unused", new AuthenticationInfo()));

        exception.Message.ShouldBe($"Git command failed with exit code 128: {message}");
    }

    [TestCase(401, "Unauthorized: Incorrect username/password")]
    [TestCase(403, "Forbidden: Possibly Incorrect username/password")]
    [TestCase(404, "Not found: The repository was not found")]
    public void CloneFailureMapsActualHttpStatusDespiteStatusCodeDigitsInUrl(int status, string expectedMessage)
    {
        var message = $"fatal: unable to access 'https://example.test:40123/403/404/': The requested URL returned error: {status}\n";

        var exception = Should.Throw<InvalidOperationException>(
            () => CreateFailingMutator(message).Clone("https://example.test/repo.git", "unused", new AuthenticationInfo()));

        exception.Message.ShouldBe(expectedMessage);
    }

    [TestCase(401, "Unauthorized: Incorrect username/password")]
    [TestCase(403, "Forbidden: Possibly Incorrect username/password")]
    [TestCase(404, "Not found: The repository was not found")]
    public void CloneFailureMapsHttpStatusInMultilineRpcDiagnostics(int status, string expectedMessage)
    {
        var message = $"Cloning into '/tmp/repo-401'...\r\nerror: RPC failed; HTTP {status} curl 22 The requested URL returned error: {status}\r\nfatal: expected flush after ref listing";

        var exception = Should.Throw<InvalidOperationException>(
            () => CreateFailingMutator(message).Clone("https://example.test/repo.git", "unused", new AuthenticationInfo()));

        exception.Message.ShouldBe(expectedMessage);
    }

    [TestCase("Authentication failed", "Unauthorized: Incorrect username/password")]
    [TestCase("Repository not found", "Not found: The repository was not found")]
    public void CloneFailureRetainsRecognizedErrorPhrases(string message, string expectedMessage)
    {
        var exception = Should.Throw<InvalidOperationException>(
            () => CreateFailingMutator(message).Clone("https://example.test/repo.git", "unused", new AuthenticationInfo()));

        exception.Message.ShouldBe(expectedMessage);
    }

    [TestCase(401)]
    [TestCase(403)]
    [TestCase(404)]
    public void CloneFailureDoesNotTreatLongerNumbersAsHttpStatus(int status)
    {
        var message = $"fatal: The requested URL returned error: {status}0";

        var exception = Should.Throw<InvalidOperationException>(
            () => CreateFailingMutator(message).Clone("https://example.test/repo.git", "unused", new AuthenticationInfo()));

        exception.Message.ShouldBe($"Git command failed with exit code 128: {message}");
    }

    [TestCase(401)]
    [TestCase(403)]
    [TestCase(404)]
    public void CheckoutFailureDoesNotClassifyHttpStatusDiagnostics(int status)
    {
        var message = $"fatal: The requested URL returned error: {status}";

        var exception = Should.Throw<InvalidOperationException>(
            () => CreateFailingMutator(message).Checkout("unused", "main"));

        exception.Message.ShouldBe($"Git command failed with exit code 128: {message}");
    }

    private static GitCliMutator CreateFailingMutator(string standardError) =>
        new(NullLogger<GitCliMutator>.Instance, new StubGitCliExecutor((_, _) => new(128, string.Empty, standardError)));

    private sealed class StubGitCliExecutor(Func<string?, IReadOnlyList<string>, GitCliResult> execute) : IGitCliExecutor
    {
        GitCliResult IGitCliExecutor.Execute(string? workingDirectory, IReadOnlyList<string> arguments) => execute(workingDirectory, arguments);
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
            _ = Task.Run(async () =>
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
