namespace GitVersion.Testing.Helpers;

[TestFixture]
public class CommitLabelGeneratorTests
{
    [TestCase("")]
    [TestCase("   ")]
    public void GetOrAdd_ShouldThrow_WhenKeyIsEmptyOrWhitespace(string key)
    {
        var sut = new CommitLabelGenerator();
        var ex = Should.Throw<ArgumentException>(() => sut.GetOrAdd(key));
        ex.Message.ShouldContain("SHA cannot be empty or whitespace");
        ex.ParamName.ShouldBe("key");
    }

    [Test]
    public void GetOrAdd_ShouldReturnNA_WhenKeyIsNA()
    {
        var sut = new CommitLabelGenerator();
        sut.GetOrAdd("N/A").ShouldBe("N/A");
    }

    [Test]
    public void GetOrAdd_ShouldAssignLabels_Sequentially()
    {
        var sut = new CommitLabelGenerator();

        sut.GetOrAdd("sha1").ShouldBe("A");
        sut.GetOrAdd("sha2").ShouldBe("B");
        sut.GetOrAdd("sha3").ShouldBe("C");
    }

    [Test]
    public void GetOrAdd_ShouldReturnSameLabel_OnRepeatedCalls()
    {
        var sut = new CommitLabelGenerator();
        sut.GetOrAdd("sha1").ShouldBe(sut.GetOrAdd("sha1"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void GetOrAddRoot_ShouldThrow_WhenKeyIsEmptyOrWhitespace(string key)
    {
        var sut = new CommitLabelGenerator();
        var ex = Should.Throw<ArgumentException>(() => sut.GetOrAddRoot(key));
        ex.Message.ShouldContain("Version source SHA cannot be empty or whitespace");
        ex.ParamName.ShouldBe("versionSourceSha");
    }

    [Test]
    public void GetOrAddRoot_ShouldReturnNA_WhenKeyIsNA()
    {
        var sut = new CommitLabelGenerator();
        sut.GetOrAddRoot("N/A").ShouldBe("N/A");
    }

    [Test]
    public void GetOrAddRoot_ShouldAssignRootLabels_Sequentially()
    {
        var sut = new CommitLabelGenerator();

        sut.GetOrAddRoot("rootsha1").ShouldBe("RootA");
        sut.GetOrAddRoot("rootsha2").ShouldBe("RootB");
        sut.GetOrAddRoot("rootsha3").ShouldBe("RootC");
    }

    [Test]
    public void GetOrAddRoot_ShouldReturnSameLabel_OnRepeatedCalls()
    {
        var sut = new CommitLabelGenerator();
        sut.GetOrAddRoot("rootsha1").ShouldBe(sut.GetOrAddRoot("rootsha1"));
    }

    [Test]
    public void GetOrAddRoot_And_GetOrAdd_ShouldMaintainSequenceLabels()
    {
        var sut = new CommitLabelGenerator();

        sut.GetOrAddRoot("sha1").ShouldBe("RootA");
        sut.GetOrAdd("sha2").ShouldBe("B");
        sut.GetOrAddRoot("sha3").ShouldBe("RootC");
    }

    [Test]
    public void GetOrAddRoot_And_GetOrAdd_ShouldAlign()
    {
        var sut = new CommitLabelGenerator();

        sut.GetOrAddRoot("sha1").ShouldBe("RootA");
        sut.GetOrAdd("sha1").ShouldBe("RootA");
        sut.GetOrAdd("sha2").ShouldBe("B");
        sut.GetOrAddRoot("sha2").ShouldBe("B");
    }
}
