using System.Collections.Generic;
using System.Linq;
using GitVersion;
using GitVersion.Extensions;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
    public class HelpWriterTests : TestBase
    {
        private readonly IHelpWriter helpWriter;

        public HelpWriterTests()
        {
            var sp = ConfigureServices(services =>
            {
                services.AddModule(new GitVersionExeModule());
            });
            helpWriter = sp.GetService<IHelpWriter>();
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
                { nameof(Arguments.Verbosity), "/verbosity" },
                { nameof(Arguments.CommitId), "/c" },
            };
            string helpText = null;

            helpWriter.WriteTo(s => helpText = s);

            var ignored = new[]
            {
                nameof(Arguments.Authentication),
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
