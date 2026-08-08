namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class TagTests
{
    [Test]
    public void ReadsAnAnnotatedTag()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var commitSha = repository.Commit("a commit");
        repository.Run("tag", "-a", "v1.2.3", "-m", "release 1.2.3");

        var tagSha = repository.RevParse("v1.2.3");
        tagSha.ShouldNotBe(commitSha, "an annotated tag should be its own object");

        using var store = repository.OpenObjectStore();
        var tag = store.GetTag(GitObjectId.Parse(tagSha));

        tag.Sha.ToString().ShouldBe(tagSha);
        tag.Name.ShouldBe("v1.2.3");
        tag.TargetType.ShouldBe("commit");
        tag.Target.ToString().ShouldBe(commitSha);
        tag.Message.ShouldBe("release 1.2.3\n");

        tag.Tagger.ShouldNotBeNull();
        tag.Tagger.Value.Name.ShouldBe(GitTestRepository.CommitterName);
        tag.Tagger.Value.Email.ShouldBe(GitTestRepository.CommitterEmail);
        tag.Tagger.Value.When.ShouldBe(repository.CurrentDate);
    }

    [Test]
    public void ReadsAnAnnotatedTagWithAMultiLineMessage()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("a commit");
        repository.Run("tag", "-a", "v2.0.0", "-m", "subject\n\nbody with details");

        using var store = repository.OpenObjectStore();
        var tag = store.GetTag(repository.ResolveId("v2.0.0"));

        tag.Message.ShouldBe("subject\n\nbody with details\n");
    }

    [Test]
    public void ReadsANestedAnnotatedTag()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("a commit");
        repository.Run("tag", "-a", "inner", "-m", "inner tag");
        repository.Run("tag", "-a", "outer", "-m", "outer tag", "inner");

        using var store = repository.OpenObjectStore();
        var outer = store.GetTag(repository.ResolveId("outer"));

        outer.TargetType.ShouldBe("tag");
        outer.Target.ToString().ShouldBe(repository.RevParse("inner"));
    }

    [Test]
    public void ReadsAnAnnotatedTagFromAPackFile()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var commitSha = repository.Commit("a commit");
        repository.Run("tag", "-a", "v3.0.0", "-m", "packed tag");
        var tagSha = repository.RevParse("v3.0.0");

        repository.Run("gc", "-q");

        using var store = repository.OpenObjectStore();
        var tag = store.GetTag(GitObjectId.Parse(tagSha));

        tag.Name.ShouldBe("v3.0.0");
        tag.Target.ToString().ShouldBe(commitSha);
        tag.Message.ShouldBe("packed tag\n");
    }
}
