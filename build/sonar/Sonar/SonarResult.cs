using System.Globalization;
using System.Text.Json;

namespace GitVersion.Sonar;

public static class SonarResult
{
    public static bool HasDecoration(JsonElement checks, JsonElement comments, RunIdentity identity, DateTimeOffset submittedAt) =>
        checks.GetProperty("check_runs").EnumerateArray().Any(c =>
            c.GetProperty("app").GetProperty("slug").GetString() == "sonarqubecloud" &&
            c.GetProperty("head_sha").GetString() == identity.HeadSha &&
            c.GetProperty("status").GetString() == "completed" &&
            DateTimeOffset.TryParse(c.GetProperty("completed_at").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var completed) && completed >= submittedAt) &&
        comments.EnumerateArray().Any(c => c.GetProperty("user").GetProperty("login").GetString() == "sonarqubecloud[bot]" &&
            DateTimeOffset.TryParse(c.GetProperty("updated_at").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var updated) && updated >= submittedAt);
}
