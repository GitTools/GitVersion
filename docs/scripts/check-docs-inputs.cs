#:project ../../build/docs/docs.csproj
#:property JsonSerializerIsReflectionEnabledByDefault=true

using Docs.Utilities;

var checks = 0;
void Check(string name, Action action)
{
    action();
    Console.WriteLine($"PASS {name}");
    checks++;
}
void Equal(string expected, string actual)
{
    if (expected != actual) throw new InvalidOperationException($"Expected {expected}, got {actual}");
}
void Reject(Action action)
{
    try { action(); }
    catch (InvalidOperationException) { return; }
    throw new InvalidOperationException("Expected invalid input to be rejected.");
}

Check("LatestPatchUsesNumericOrder", () => Equal("6.8.12", DocsInputs.LatestPatch("6.8", ["6.8.9", "6.8.12", "6.8.2"])));
Check("LatestPatchExcludesOtherTrainsAndPrereleases", () => Equal("5.12.0", DocsInputs.LatestPatch("5.12", ["5.12.0", "5.12.1-rc.1", "5.13.0", "6.12.9"])));
Check("LatestPatchRejectsMissingRelease", () => Reject(() => DocsInputs.LatestPatch("6.8", ["6.9.0", "6.8.0-beta.1"])));
Check("LatestPatchRejectsEmptyFeed", () => Reject(() => DocsInputs.LatestPatch("5.12", [])));
Check("LinkPathDropsQueryAndFragment", () => Equal("/7.0/docs", DocsLinks.PathOnly("/7.0/docs?mode=light#install")));
Check("LinkPathDropsFragmentBeforeQuery", () => Equal("/7.0/docs", DocsLinks.PathOnly("/7.0/docs#install?mode=light")));

var temporary = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
try
{
    void Manifest(string editions) => File.WriteAllText(temporary, "{\"versions\":[" + editions + "]}");
    const string current = "{\"id\":\"5.12\",\"label\":\"5.12\",\"basePath\":\"/5.12\",\"status\":\"release\",\"framework\":\"net6.0\"}";
    const string release = "{\"id\":\"6.8\",\"label\":\"6.8\",\"basePath\":\"/6.8\",\"status\":\"release\",\"framework\":\"net8.0\"}";
    Check("ManifestAcceptsSeparateEditions", () => { Manifest(current + "," + release); Equal("2", DocsInputs.ReadManifest(temporary).Versions.Length.ToString()); });
    Check("ManifestAcceptsOnlyReleases", () => { Manifest(release); Equal("1", DocsInputs.ReadManifest(temporary).Versions.Length.ToString()); });
    Check("ManifestRejectsDuplicateIds", () => { Manifest(current + "," + release + "," + release); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsOverlappingAlias", () => { Manifest(current + "," + release[..^1] + ",\"aliases\":[\"/6.8\"]}"); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsPathTraversal", () => { Manifest(current + "," + release.Replace("/6.8", "/../6.8")); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsReservedPaths", () => { Manifest(current + "," + release.Replace("/6.8", "/docs")); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsPatchSelector", () => { Manifest(current + "," + release.Replace("6.8", "6.8.2")); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestAllowsFrameworkDiscovery", () => { Manifest(release.Replace(",\"framework\":\"net8.0\"", "")); Equal("1", DocsInputs.ReadManifest(temporary).Versions.Length.ToString()); });
    Check("DiscoverySortsNumericallyAndKeepsOlderMinors", () =>
    {
        Manifest(current + "," + release);
        var editions = DocsInputs.ResolveEditions(DocsInputs.ReadManifest(temporary), ["7.2.0", "7.10.0", "7.2.1", "8.0.0-rc.1", "6.9.0"]);
        Equal("7.10,7.2,6.8,5.12", string.Join(',', editions.Select(e => e.Id)));
    });
    Check("DiscoveryDefaultsTo68BeforeStable7", () =>
    {
        Manifest(current + "," + release);
        Equal("6.8", DocsInputs.ResolveEditions(DocsInputs.ReadManifest(temporary), ["7.0.0-beta.1"])[0].Id);
    });
    Check("DiscoveryHonorsExplicitOverrides", () =>
    {
        Manifest(release.Replace("6.8", "7.0"));
        Equal("net8.0", DocsInputs.ResolveEditions(DocsInputs.ReadManifest(temporary), ["7.0.0"])[0].Framework!);
    });
    Check("DevelopmentEditionRemainsVisibleBeforeRelease", () =>
    {
        Manifest(release.Replace("6.8", "7.0").Replace("release", "development"));
        var edition = DocsInputs.ResolveEditions(DocsInputs.ReadManifest(temporary), ["7.0.0-beta.1"])[0];
        Equal("7.0", edition.Id);
        Equal("development", edition.Status);
    });
    Check("DevelopmentEditionSwitchesToStableInputs", () =>
    {
        Manifest(release.Replace("6.8", "7.0").Replace("release", "development"));
        Equal("release", DocsInputs.ResolveEditions(DocsInputs.ReadManifest(temporary), ["7.0.0"])[0].Status);
    });
}
finally { File.Delete(temporary); }
Console.WriteLine($"Passed: {checks}, Failed: 0");
