using System.Text.RegularExpressions;
using static GitVersion.Core.RegexPatterns.AssemblyVersion;

namespace GitVersion.Core.Continuous.Tests
{
    public class RegexPatternsTests
    {
        [TestCase("/foo:", true, "/foo:")]
        [TestCase("/bar:", true, "/bar:")]
        [TestCase("foo:", false, null)]
        public void SwitchArgumentRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Common.SwitchArgumentRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("https://user:pass@host", true, "https://user:pass@")]
        [TestCase("http://user:pass@host", true, "http://user:pass@")]
        [TestCase("ftp://user:pass@host", false, null)]
        public void ObscurePasswordRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Common.ObscurePasswordRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("{env:FOO}", true, "{env:FOO}")]
        [TestCase("{bar}", true, "{bar}")]
        [TestCase("{env:FOO ?? \"fallback\"}", true, "{env:FOO ?? \"fallback\"}")]
        [TestCase("{bar ?? fallback}", true, "{bar ?? fallback}")]
        [TestCase("env:FOO", false, null)]
        public void ExpandTokensRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Common.ExpandTokensRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("v", true, "v")]
        [TestCase("V", true, "V")]
        [TestCase("v1", true, "v")]
        [TestCase("V2.0", true, "V")]
        [TestCase("1", true, "")]
        [TestCase("x", true, "")]
        [TestCase("", true, "")]
        public void DefaultTagPrefixRegex_MatchesExpected(string input, bool expected, string expectedCapture)
        {
            var match = RegexPatterns.Configuration.DefaultTagPrefixRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("v1.2.3", true, "v1.2.3")]
        [TestCase("1.2", true, "1.2")]
        [TestCase("main", false, null)]
        public void DefaultVersionInBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.DefaultVersionInBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("main", true, "main")]
        [TestCase("master", true, "master")]
        [TestCase("develop", false, null)]
        public void MainBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.MainBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("develop", true, "develop")]
        [TestCase("development", true, "development")]
        [TestCase("dev", true, "dev")]
        [TestCase("main", false, null)]
        public void DevelopBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.DevelopBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("release/1.0", true, "release/1.0")]
        [TestCase("releases-2.0", true, "releases-2.0")]
        [TestCase("feature/foo", false, null)]
        public void ReleaseBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.ReleaseBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("feature/foo", true, "feature/foo")]
        [TestCase("features-bar", true, "features-bar")]
        [TestCase("hotfix/1.0", false, null)]
        public void FeatureBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.FeatureBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("pull-requests/123", true, "pull-requests/123")]
        [TestCase("pull-123", true, "pull-123")]
        [TestCase("pr-456", true, "pr-456")]
        [TestCase("main", false, null)]
        public void PullRequestBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.PullRequestBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("hotfix/1.0", true, "hotfix/1.0")]
        [TestCase("hotfixes-2.0", true, "hotfixes-2.0")]
        [TestCase("support/1.0", false, null)]
        public void HotfixBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.HotfixBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("support/1.0", true, "support/1.0")]
        [TestCase("support-2.0", true, "support-2.0")]
        [TestCase("main", false, null)]
        public void SupportBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.SupportBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("any-branch", true, "any-branch")]
        [TestCase("main", true, "main")]
        public void UnknownBranchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Configuration.UnknownBranchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("Merge branch 'feature/foo' into develop", true, "Merge branch 'feature/foo' into develop")]
        [TestCase("Merge tag 'v1.0.0'", true, "Merge tag 'v1.0.0'")]
        [TestCase("Finish feature/foo", false, null)]
        public void DefaultMergeMessageRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.MergeMessage.DefaultMergeMessageRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("Finish feature/foo into develop", true, "Finish feature/foo into develop")]
        [TestCase("Finish bugfix/bar", true, "Finish bugfix/bar")]
        [TestCase("Merge branch 'feature/foo'", false, null)]
        public void SmartGitMergeMessageRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.MergeMessage.SmartGitMergeMessageRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("Merge pull request #123 from repo from feature/foo to develop", true, "Merge pull request #123 from repo from feature/foo to develop")]
        [TestCase("Merge pull request #1 in repo from bugfix/bar to main", true, "Merge pull request #1 in repo from bugfix/bar to main")]
        [TestCase("Finish feature/foo", false, null)]
        public void BitBucketPullMergeMessageRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.MergeMessage.BitBucketPullMergeMessageRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("Pull request #123\n\nMerge in repo from feature/foo to develop", true, "Pull request #123\n\nMerge in repo from feature/foo to develop")]
        [TestCase("Pull request #1\n\nMerge in repo from bugfix/bar to main", true, "Pull request #1\n\nMerge in repo from bugfix/bar to main")]
        [TestCase("Merge pull request #123 from repo from feature/foo to develop", false, null)]
        public void BitBucketPullv7MergeMessageRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.MergeMessage.BitBucketPullv7MergeMessageRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("Merged in feature/foo (pull request #123)", true, "Merged in feature/foo (pull request #123)")]
        [TestCase("Merged in bugfix/bar (pull request #1)", true, "Merged in bugfix/bar (pull request #1)")]
        [TestCase("Merge pull request #123 from repo from feature/foo to develop", false, null)]
        public void BitBucketCloudPullMergeMessageRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.MergeMessage.BitBucketCloudPullMergeMessageRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("Merge pull request #123 from repo/feature/foo", false, null)]
        [TestCase("Merge pull request #123 from feature/foo into develop", false, null)]
        public void AzureDevOpsPullMergeMessageRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.MergeMessage.AzureDevOpsPullMergeMessageRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("[assembly: AssemblyVersion(\"1.0.0.0\")]", true, "[assembly: AssemblyVersion(\"1.0.0.0\")]")]
        [TestCase("[assembly: AssemblyFileVersion(\"1.0.0.0\")]", true, "[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
        [TestCase("[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]", true, "[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]")]
        [TestCase("random text", false, null)]
        public void CsharpAssemblyAttributeRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Output.CsharpAssemblyAttributeRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("[<assembly: AssemblyVersion(\"1.0.0.0\")>]", true, "[<assembly: AssemblyVersion(\"1.0.0.0\")>]")]
        [TestCase("[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]", true, "[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
        [TestCase("[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]", true, "[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]")]
        [TestCase("random text", false, null)]
        public void FsharpAssemblyAttributeRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Output.FsharpAssemblyAttributeRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("<Assembly: AssemblyVersion(\"1.0.0.0\")>", true, "<Assembly: AssemblyVersion(\"1.0.0.0\")>")]
        [TestCase("<Assembly: AssemblyFileVersion(\"1.0.0.0\")>", true, "<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
        [TestCase("<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>", true, "<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>")]
        [TestCase("random text", false, null)]
        public void VisualBasicAssemblyAttributeRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.Output.VisualBasicAssemblyAttributeRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("+semver: major", true, "+semver: major")]
        [TestCase("+semver: breaking", true, "+semver: breaking")]
        [TestCase("+semver: minor", false, null)]
        public void DefaultMajorRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.VersionCalculation.DefaultMajorRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("+semver: minor", true, "+semver: minor")]
        [TestCase("+semver: feature", true, "+semver: feature")]
        [TestCase("+semver: patch", false, null)]
        public void DefaultMinorRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.VersionCalculation.DefaultMinorRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("+semver: patch", true, "+semver: patch")]
        [TestCase("+semver: fix", true, "+semver: fix")]
        [TestCase("+semver: none", false, null)]
        public void DefaultPatchRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.VersionCalculation.DefaultPatchRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("+semver: none", true, "+semver: none")]
        [TestCase("+semver: skip", true, "+semver: skip")]
        [TestCase("+semver: patch", false, null)]
        public void DefaultNoBumpRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.VersionCalculation.DefaultNoBumpRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("1.2.3", true, "1.2.3")]
        [TestCase("1.2.3-alpha", true, "1.2.3-alpha")]
        [TestCase("1.2", false, null)]
        public void ParseStrictRegex_MatchesExpected(string input, bool expected, string? expectedCapture)
        {
            var match = RegexPatterns.SemanticVersion.ParseStrictRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("1.2.3", true, "1.2.3")]
        [TestCase("1.2", true, "1.2")]
        [TestCase("1", true, "1")]
        [TestCase("1.2.3.4", true, "1.2.3.4")]
        public void ParseLooseRegex_MatchesExpected(string input, bool expected, string expectedCapture)
        {
            var match = RegexPatterns.SemanticVersion.ParseLooseRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("123.BranchName.foo.Sha.abc", true, "123.BranchName.foo.Sha.abc")]
        [TestCase("Branch.Name.develop", true, "Branch.Name.develop")]
        [TestCase("random", true, "random")]
        public void ParseBuildMetaDataRegex_MatchesExpected(string input, bool expected, string expectedCapture)
        {
            var match = RegexPatterns.SemanticVersion.ParseBuildMetaDataRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("meta-data", false, new string[0])]
        [TestCase("meta_data", true, new[] { "_" })]
        [TestCase("meta.data", false, new string[0])]
        [TestCase("meta+data$", true, new[] { "+", "$" })]
        [TestCase("m@e#t!a", true, new[] { "@", "#", "!" })]
        public void FormatBuildMetaDataRegex_CapturesInvalidCharacters(string input, bool shouldMatch, string[] expectedCaptures)
        {
            var matches = RegexPatterns.SemanticVersion.FormatBuildMetaDataRegex().Matches(input);

            var matched = matches.Count > 0;
            matched.ShouldBe(shouldMatch, $"Expected match: {shouldMatch}, but found {matches.Count} matches.");

            var captured = new List<string>();
            foreach (Match m in matches)
                captured.Add(m.Value);

            captured.ShouldBe(expectedCaptures);
        }

        [TestCase("alpha.1", true, "alpha.1")]
        [TestCase("beta", true, "beta")]
        [TestCase("rc.2", true, "rc.2")]
        public void ParsePreReleaseTagRegex_MatchesExpected(string input, bool expected, string expectedCapture)
        {
            var match = RegexPatterns.SemanticVersion.ParsePreReleaseTagRegex().Match(input);
            match.Success.ShouldBe(expected);
            if (expected)
                match.Value.ShouldBe(expectedCapture);
        }

        [TestCase("/* block comment */", true, "block", " block comment ")]
        [TestCase("// line comment\r\n", true, "line", " line comment")]
        [TestCase("\"string \\\"text\\\" inside\"", true, "str", "string \\\"text\\\" inside")]
        [TestCase("int x = 5;", false, "", "")]
        public void CSharpTriviaRegex_CapturesExpected(string input, bool expectedMatch, string expectedGroup, string expectedGroupValue)
        {
            var regex = CSharp.TriviaRegex();
            var match = regex.Match(input);
            match.Success.ShouldBe(expectedMatch);

            if (expectedMatch && !string.IsNullOrEmpty(expectedGroup))
            {
                // Match group 1: block, group 2: line, group 3: string
                var groups = match.Groups;
                var actualGroupValue = expectedGroup switch
                {
                    "block" => groups[1].Success ? groups[1].Value : "",
                    "line" => groups[2].Success ? groups[2].Value : "",
                    "str" => groups[3].Success ? groups[3].Value : "",
                    _ => ""
                };
                actualGroupValue.ShouldBe(expectedGroupValue);
            }
        }

        [TestCase("[assembly: AssemblyVersion()]", true)]
        [TestCase("[assembly:AssemblyFileVersion()]", true)]
        [TestCase("[assembly:System.Reflection.AssemblyInformationalVersion()]", true)]
        [TestCase("[assembly: AssemblyTitle(\"App\")]", false)]
        public void CSharpAttributeRegex_MatchesExpected(string input, bool expectedMatch)
        {
            var regex = CSharp.AttributeRegex();
            var match = regex.Match(input);
            match.Success.ShouldBe(expectedMatch);
        }

        [TestCase("/* block comment */", true, "block", " block comment ")]
        [TestCase("// line comment\r\n", true, "line", " line comment")]
        [TestCase("\"string \\\"text\\\" inside\"", true, "str", "string \\\"text\\\" inside")]
        [TestCase("let x = 1", false, "", "")]
        public void FSharpTriviaRegex_CapturesExpected(string input, bool expectedMatch, string expectedGroup, string expectedGroupValue)
        {
            var regex = FSharp.TriviaRegex();
            var match = regex.Match(input);
            match.Success.ShouldBe(expectedMatch);

            if (expectedMatch && !string.IsNullOrEmpty(expectedGroup))
            {
                var groups = match.Groups;
                var actualGroupValue = expectedGroup switch
                {
                    "block" => groups[1].Success ? groups[1].Value : "",
                    "line" => groups[2].Success ? groups[2].Value : "",
                    "str" => groups[3].Success ? groups[3].Value : "",
                    _ => ""
                };
                actualGroupValue.ShouldBe(expectedGroupValue);
            }
        }

        [TestCase("[<assembly: AssemblyVersion()>]", true)]
        [TestCase("[<assembly:AssemblyFileVersion()>]", true)]
        [TestCase("[<assembly:System.Reflection.AssemblyInformationalVersion()>]", true)]
        [TestCase("[<assembly: AssemblyTitle(\"Test\")>]", false)]
        [TestCase("[assembly: AssemblyVersion()]", false)]
        public void FSharpAttributeRegex_MatchesExpected(string input, bool expectedMatch)
        {
            var regex = FSharp.AttributeRegex();
            var match = regex.Match(input);
            match.Success.ShouldBe(expectedMatch);
        }
    }
}
