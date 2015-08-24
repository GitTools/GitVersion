using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using GitVersion;
using GitVersion.Options;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class ArgumentParserTests
{
    [TestCaseSource("AllVerbs")]
    public void PrintVerbHelp(string verb)
    {
        Parser.Default.ParseArguments(new[] {"help", verb}, AllOptionTypes().ToArray());
    }

    IEnumerable<string> AllVerbs()
    {
        return AllOptionTypes()
            .Select(t => (VerbAttribute) Attribute.GetCustomAttribute(t, typeof(VerbAttribute)))
            .Where(a => a != null).Select(a => a.Name);
    }

    IEnumerable<Type> AllOptionTypes()
    {
        yield return typeof(InspectOptions);
        yield return typeof(InitOptions);
        yield return typeof(InspectRemoteRepositoryOptions);
        yield return typeof(InjectBuildServerOptions);
        yield return typeof(InjectMsBuildOptions);
        yield return typeof(InjectProcess);
        yield return typeof(InjectAssemblyInfo);
    }

    [Test]
    public void InputVariablesMustHaveCorrectDefaultValues()
    {
        var iv = new InputVariables();

        iv.TargetUrl.ShouldBe(null);

        iv.DynamicRepositoryLocation.ShouldBe(null);

        iv.Authentication.ShouldNotBe(null);

        var defaultAuthentication = new Authentication();
        iv.Authentication.Username.ShouldBeSameAs(defaultAuthentication.Username);
        iv.Authentication.Password.ShouldBeSameAs(defaultAuthentication.Password);

        iv.TargetUrl.ShouldBe(null);

        iv.TargetBranch.ShouldBe(null);

        iv.NoFetch.ShouldBe(true);

        iv.TargetPath.ShouldBe(Environment.CurrentDirectory);
    }

    [Explicit]
    [Test]
    public void PrintEntireHelp()
    {
        Parser.Default.ParseArguments(new[] {"help"}, AllOptionTypes().ToArray());
        foreach (var verb in AllVerbs())
        {
            PrintVerbHelp(verb);
        }
    }

}