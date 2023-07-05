namespace GitVersion.Testing.Internal;

internal static class PathHelper
{
    public static string GetCurrentDirectory() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException();
    public static string GetTempPath() => Path.Combine(GetCurrentDirectory(), "TestRepositories", Guid.NewGuid().ToString());
}
