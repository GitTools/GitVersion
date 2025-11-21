using Microsoft.CodeAnalysis.Testing;

namespace GitVersion.Cli.Generator.Tests;

public static class Extensions
{
    private static readonly Lazy<ReferenceAssemblies> LazyNet100 = new(() =>
        new("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0")));

    extension(ReferenceAssemblies.Net)
    {
        public static ReferenceAssemblies Net100 => LazyNet100.Value;
    }
}
