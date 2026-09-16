namespace GitVersion.Extensions;

internal static class EnvironmentExtensions
{
    extension(IEnvironment environment)
    {
        public string? GetFirstNonBlankEnvironmentVariable(params IEnumerable<string> names) => names
            .Select(environment.GetEnvironmentVariable)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
