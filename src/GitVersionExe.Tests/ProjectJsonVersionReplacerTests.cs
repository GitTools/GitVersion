namespace GitVersionExe.Tests
{
    using System;
    using System.Runtime.CompilerServices;
    using GitVersion;
    using GitVersionCore.Tests;
    using NUnit.Framework;
    using Shouldly;

    public class ProjectJsonVersionReplacerTests
    {

        [SetUp]
        public void SetUp()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void ShouldOnlyReplaceRootVersionValue()
        {
            var json =
@"{
  ""dependencies"": {
    ""Microsoft.NETCore.App"": {
         ""version"": ""1.0.0""
    }
  },
  ""version"": ""1.1.0"",
  ""runtimes"": {
    ""win8-x64"": {}
  }
}
";
            var result = ProjectJsonVersionReplacer.Replace(json, GetVariables());
            result.HasError.ShouldBe(false);
            result.VersionElementNotFound.ShouldBe(false);
            result.JsonWithReplacement.ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void ShouldReplaceSuccessfullyWhenValueIsNull()
        {
            var json =
@"{
  ""dependencies"": {
    ""Microsoft.NETCore.App"": {
         ""version"": ""1.0.0""
    }
  },
  ""version"": null,
  ""runtimes"": {
    ""win8-x64"": {}
  }
}
";
            var result = ProjectJsonVersionReplacer.Replace(json, GetVariables());
            result.HasError.ShouldBe(false);
            result.VersionElementNotFound.ShouldBe(false);
            result.JsonWithReplacement.ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void ShouldWorkAndPreserveFormattingInWeirdlyFormattedJson()
        {
            var json =
@"{
  ""dependencies""
        : 
{
    ""Microsoft.NETCore.App"": {
    ""version"": ""1.0.0""
    }
  },
            ""version""
    :
""1.1.0""

,


""runtimes"": {
    ""win8-x64"": {}
  }
}
";
            var result = ProjectJsonVersionReplacer.Replace(json, GetVariables());
            result.HasError.ShouldBe(false);
            result.VersionElementNotFound.ShouldBe(false);
            result.JsonWithReplacement.ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        public void ShouldNotBombOutOnMalformedJson()
        {
            var json =
@"{
  ""dependencies: {
    ""Microsoft.NETCore.App"": {
         ""version"": ""1.0.0""
    }
  },
  ""version"": ""1.1.0"",
  ""runtimes"": {
    ""win8-x64"": {}
  }
}
";
            var result = ProjectJsonVersionReplacer.Replace(json, GetVariables());
            result.HasError.ShouldBe(true);
            result.Error.ShouldBe("Invalid character after parsing property name. Expected ':' but got: M. Path '', line 3, position 5.");
            result.VersionElementNotFound.ShouldBe(false);
        }

        [Test]
        public void ShouldIndicateWhenNoVersionElementWasFound()
        {
            var json =
@"{
  ""dependencies"": {
    ""Microsoft.NETCore.App"": {
         ""version"": ""1.0.0""
    }
  },
  ""runtimes"": {
    ""win8-x64"": {}
  }
}
";
            var result = ProjectJsonVersionReplacer.Replace(json, GetVariables());
            result.HasError.ShouldBe(false);
            result.VersionElementNotFound.ShouldBe(true);
        }



        private static VersionVariables GetVariables()
        {
            SemanticVersion semVer = new SemanticVersion(1, 2, 3)
            {
                BuildMetaData = new SemanticVersionBuildMetaData(2, "alpha", "ADF", new DateTimeOffset(2011, 2, 3, 4, 5, 6, 7, TimeSpan.FromHours(2)), "bar"),
                PreReleaseTag = new SemanticVersionPreReleaseTag("foo", 4)
            };
            return VariableProvider.GetVariablesFor(semVer, new TestEffectiveConfiguration(), true);
        }
    }
}