using Microsoft.CodeAnalysis.Testing;

namespace GitVersion.Cli.Generator.Tests;

public static class Extensions
{
    extension(ReferenceAssemblies)
    {
        public static ReferenceAssemblies Net10 =>
            new("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0"));
    }
}
