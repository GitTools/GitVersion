using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GitVersion.App.Tests;

[TestFixture]
public class TelemetryReleaseDateTests
{
    [Test]
    public void TryGetReleaseDateReturnsTrueWhenMetadataExists()
    {
        var assembly = GenerateAssembly("""[assembly: System.Reflection.AssemblyMetadata("GitVersionReleaseDate", "2026-04-26")]""");

        var result = TelemetryReleaseDate.TryGetReleaseDate(assembly, out var releaseDate);

        result.ShouldBe(true);
        releaseDate.ShouldBe(new DateOnly(2026, 04, 26));
    }

    [Test]
    public void TryGetReleaseDateReturnsFalseWhenMetadataIsMissing()
    {
        var assembly = GenerateAssembly(string.Empty);

        var result = TelemetryReleaseDate.TryGetReleaseDate(assembly, out _);

        result.ShouldBe(false);
    }

    [Test]
    public void TryGetReleaseDateReturnsFalseWhenMetadataHasInvalidFormat()
    {
        var assembly = GenerateAssembly("""[assembly: System.Reflection.AssemblyMetadata("GitVersionReleaseDate", "2026-04-26T15:00:00Z")]""");

        var result = TelemetryReleaseDate.TryGetReleaseDate(assembly, out _);

        result.ShouldBe(false);
    }

    [Test]
    public void IsWithinWindowReturnsTrueBeforeThreeMonths() =>
        TelemetryReleaseDate.IsWithinWindow(new DateOnly(2026, 04, 26), new DateOnly(2026, 07, 25)).ShouldBe(true);

    [Test]
    public void IsWithinWindowReturnsFalseAtThreeMonths() =>
        TelemetryReleaseDate.IsWithinWindow(new DateOnly(2026, 04, 26), new DateOnly(2026, 07, 26)).ShouldBe(false);

    private static Assembly GenerateAssembly(string attributes) => Assembly.Load(CompileAssembly(attributes).ToArray());

    private static MemoryStream CompileAssembly(string attributes)
    {
        var compilation = CSharpCompilation.Create("test-telemetry-release-date")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(AssemblyMetadataAttribute).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(attributes));

        var memoryStream = new MemoryStream();
        compilation.Emit(memoryStream);
        return memoryStream;
    }
}
