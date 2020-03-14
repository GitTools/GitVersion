using System.Collections.Generic;
using System.Linq;
using GitVersion;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
    public class HelpWriterTests
    {
        private readonly IHelpWriter helpWriter;

        public HelpWriterTests()
        {
            var versionWriter = new VersionWriter();
            helpWriter = new HelpWriter(versionWriter);
        }

        [Test]
        public void AllArgsAreInHelp()
        {
            var lookup = new Dictionary<string, string>
            {
                { nameof(Arguments.TargetUrl), "/url" },
                { nameof(Arguments.Init), "init" },
                { nameof(Arguments.TargetBranch), "/b" },
                { nameof(Arguments.LogFilePath) , "/l" },
                { nameof(Arguments.DynamicRepositoryClonePath), "/dynamicRepoLocation" },
                { nameof(Arguments.IsHelp), "/?" },
                { nameof(Arguments.IsVersion), "/version" },
                { nameof(Arguments.UpdateWixVersionFile), "/updatewixversionfile" },
                { nameof(Arguments.ConfigFile), "/config" },
            };
            string helpText = null;

            helpWriter.WriteTo(s => helpText = s);

            var ignored = new[]
            {
                nameof(Arguments.Authentication),
                nameof(Arguments.CommitId),
                nameof(Arguments.DynamicGitRepositoryPath),
                nameof(Arguments.HasOverrideConfig)
            };
            typeof(Arguments).GetFields()
                .Select(p => p.Name)
                .Where(p => IsNotInHelp(lookup, p, helpText))
                .Except(ignored)
                .ShouldBeEmpty();
        }

        private static bool IsNotInHelp(IReadOnlyDictionary<string, string> lookup, string propertyName, string helpText)
        {
            if (lookup.ContainsKey(propertyName))
                return !helpText.Contains(lookup[propertyName]);

            return !helpText.Contains("/" + propertyName.ToLower());
        }
    }
}
