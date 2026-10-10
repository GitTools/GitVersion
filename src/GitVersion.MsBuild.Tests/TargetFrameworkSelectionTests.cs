using Microsoft.Build.Evaluation;

namespace GitVersion.MsBuild.Tests;

[TestFixture]
public class TargetFrameworkSelectionTests
{
    [TestCase("net11.0", "net11.0")]
    [TestCase("net11.0-windows", "net11.0")]
    [TestCase("net12.0", "net11.0")]
    [TestCase("net10.0", "net10.0")]
    [TestCase("net8.0", "net10.0")]
    [TestCase("netstandard2.0", "net10.0")]
    [TestCase("net48", "net10.0")]
    [TestCase("", "net10.0")]
    public void SelectsCompatibleToolsForConsumer(string consumerFramework, string expectedFramework)
    {
        using var collection = new ProjectCollection();
        var project = LoadTargets(collection, consumerFramework);

        project.GetPropertyValue("GitVersionTargetFramework").ShouldBe(expectedFramework);
        project.GetPropertyValue("GitVersionAssemblyFile")
            .ShouldEndWith(Path.Combine(expectedFramework, "GitVersion.MsBuild.dll"));
        project.GetPropertyValue("GitVersionFileExe")
            .ShouldBe($"dotnet exec --roll-forward Major \"{Path.Combine(TestContext.CurrentContext.TestDirectory, "msbuild", expectedFramework, "gitversion.dll")}\"");
    }

    [TestCase("net11.0", "net10.0")]
    [TestCase("net10.0", "net11.0")]
    public void PreservesExplicitFrameworkOverride(string consumerFramework, string toolsFramework)
    {
        using var collection = new ProjectCollection();
        var project = LoadTargets(collection, consumerFramework, toolsFramework);

        project.GetPropertyValue("GitVersionTargetFramework").ShouldBe(toolsFramework);
        project.GetPropertyValue("GitVersionAssemblyFile")
            .ShouldEndWith(Path.Combine(toolsFramework, "GitVersion.MsBuild.dll"));
    }

    private static Project LoadTargets(ProjectCollection collection, string consumerFramework, string? toolsFramework = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["TargetFramework"] = consumerFramework
        };
        if (toolsFramework != null)
        {
            properties["GitVersionTargetFramework"] = toolsFramework;
        }

        var targetsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "msbuild", "GitVersion.MsBuild.targets");
        return collection.LoadProject(targetsPath, properties, null);
    }
}
