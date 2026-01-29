using GitVersion.Logging;

namespace GitVersion.Core.Tests;

[TestFixture]
public class SensitiveDataEnricherTests
{
    [Test]
    [TestCase("http")]
    [TestCase("https")]
    public void MaskPassword_ObscuresPasswordInUrl(string protocol)
    {
        const string username = "username%40domain.com";
        const string password = "password";
        var url = $"{protocol}://{username}:{password}@workspace.visualstudio.com/DefaultCollection/_git/CAS";

        var result = SensitiveDataEnricher.MaskPassword(url);

        result.ShouldNotContain(password);
        result.ShouldContain("*******");
        result.ShouldContain(username);
    }

    [Test]
    public void MaskPassword_UsernameWithoutPassword_RemainsUnchanged()
    {
        const string repoUrl = "https://username@workspace.visualstudio.com/DefaultCollection/_git/CAS";

        var result = SensitiveDataEnricher.MaskPassword(repoUrl);

        // URL without password should remain unchanged
        result.ShouldBe(repoUrl);
    }

    [Test]
    public void MaskPassword_PreservesUrlStructureWhileMasking()
    {
        const string url = "https://user:secret123@github.com/org/repo.git";
        const string expectedMasked = "https://user:*******@github.com/org/repo.git";

        var result = SensitiveDataEnricher.MaskPassword(url);

        result.ShouldBe(expectedMasked);
    }

    [Test]
    public void MaskPassword_PlainTextWithoutUrl_RemainsUnchanged()
    {
        const string text = "Just some plain text without any URL";

        var result = SensitiveDataEnricher.MaskPassword(text);

        result.ShouldBe(text);
    }
}
