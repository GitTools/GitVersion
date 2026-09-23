using System.Xml.Linq;

namespace GitVersion.Sonar.Tests;

[TestFixture]
public class ProjectIdsTests
{
    [Test]
    public void WriteProducesStableDistinctIdsAcrossCheckoutsForSameNamedProjects()
    {
        var root = Directory.CreateTempSubdirectory("sonar-project-tests-").FullName;
        try
        {
            var first = WriteProjects(Path.Combine(root, "first"));
            var second = WriteProjects(Path.Combine(root, "second"));
            Assert.That(first, Has.Count.EqualTo(3));
            Assert.That(first.Distinct().Count(), Is.EqualTo(3), "Same file names in different solutions need distinct IDs.");
            Assert.That(second, Is.EqualTo(first), "Absolute checkout location must not influence project identity.");
        }
        finally { Directory.Delete(root, true); }
    }

    private static List<string> WriteProjects(string repository)
    {
        foreach (var path in new[] { "src/Library/Library.csproj", "new-cli/Library/Library.csproj", "build/Tool/Tool.csproj", "docs/Excluded.csproj", "src/obj/Generated.csproj" })
        {
            var file = Path.Combine(repository, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "<Project/>");
        }
        var destination = Path.Combine(repository, "ids.props");
        ProjectIds.Write(repository, destination);
        return XDocument.Load(destination).Descendants("ProjectGuid").Select(e => e.Value).Order().ToList();
    }
}
