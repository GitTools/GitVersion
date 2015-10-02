using System.Collections.Generic;
using System.Linq;
using GitVersion;
using NUnit.Framework;
using Shouldly;

public class HelpWriterTests
{
    [Test]
    public void AllArgsAreInHelp()
    {
        var lookup = new Dictionary<string, string>
        {
            { "TargetUrl", "/url" },
            { "Init", "init" },
            { "TargetBranch", "/b" },
            { "LogFilePath" , "/l" },
            { "DynamicRepositoryLocation" , "/dynamicRepoLocation" },
            { "IsHelp", "/?" }
        };
        string helpText = null;

        HelpWriter.WriteTo(s => helpText = s);

        typeof(Arguments).GetFields()
            .Select(p => p.Name)
            .Where(p => IsNotInHelp(lookup, p, helpText))
            .Except(new[] { "Authentication", "CommitId" })
            .ShouldBeEmpty();
    }

    static bool IsNotInHelp(Dictionary<string, string> lookup, string propertyName, string helpText)
    {
        if (lookup.ContainsKey(propertyName))
            return !helpText.Contains(lookup[propertyName]);

        return !helpText.Contains("/" + propertyName.ToLower());
    }
}