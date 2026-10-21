using GitVersion.Testing.Extensions;

namespace GitVersion.Tests.Extensions;

[TestFixture]
public class GitTestExtensionsTests
{
    [Test]
    public void ExecuteGitCmdPreservesConcurrentStandardOutputAndError()
    {
        const int lineCount = 10000;
        const string command = "-c \"alias.test-output=!i=0; while test $i -lt 10000; do echo stdout-$i; echo stderr-$i >&2; i=$((i+1)); done\" test-output";
        string? output = null;

        GitTestExtensions.ExecuteGitCmd(command, ".", text => output = text);

        var expected = Enumerable.Range(0, lineCount)
            .SelectMany(index => new[] { $"stdout-{index}", $"ERROR: stderr-{index}" })
            .OrderBy(line => line, StringComparer.Ordinal);
        output.ShouldNotBeNull().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(line => line, StringComparer.Ordinal).ShouldBe(expected);
    }
}
