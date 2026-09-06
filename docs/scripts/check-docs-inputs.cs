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
    if (expected != actual) throw new Exception($"Expected {expected}, got {actual}");
}
void Reject(Action action)
{
    try { action(); }
    catch (InvalidOperationException) { return; }
    throw new Exception("Expected invalid input to be rejected.");
}

Check("LatestPatchUsesNumericOrder", () => Equal("6.8.12", DocsInputs.LatestPatch("6.8", ["6.8.9", "6.8.12", "6.8.2"])));
Check("LatestPatchExcludesOtherTrainsAndPrereleases", () => Equal("5.12.0", DocsInputs.LatestPatch("5.12", ["5.12.0", "5.12.1-rc.1", "5.13.0", "6.12.9"])));
Check("LatestPatchRejectsMissingRelease", () => Reject(() => DocsInputs.LatestPatch("6.8", ["6.9.0", "6.8.0-beta.1"])));
Check("LatestPatchRejectsEmptyFeed", () => Reject(() => DocsInputs.LatestPatch("5.12", [])));

var temporary = Path.GetTempFileName();
try
{
    void Manifest(string editions) => File.WriteAllText(temporary, "{\"versions\":[" + editions + "]}");
    const string current = "{\"id\":\"current\",\"label\":\"Current\",\"basePath\":\"\",\"status\":\"development\"}";
    const string release = "{\"id\":\"6.8\",\"label\":\"6.8\",\"basePath\":\"/6.8\",\"status\":\"release\",\"framework\":\"net8.0\"}";
    Check("ManifestAcceptsSeparateEditions", () => { Manifest(current + "," + release); Equal("2", DocsInputs.ReadManifest(temporary).Versions.Length.ToString()); });
    Check("ManifestRejectsMissingCurrent", () => { Manifest(release); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsDuplicateIds", () => { Manifest(current + "," + release + "," + release); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsOverlappingAlias", () => { Manifest(current + "," + release[..^1] + ",\"aliases\":[\"/6.8\"]}"); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsPathTraversal", () => { Manifest(current + "," + release.Replace("/6.8", "/../6.8")); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsReservedPaths", () => { Manifest(current + "," + release.Replace("/6.8", "/docs")); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsPatchSelector", () => { Manifest(current + "," + release.Replace("6.8", "6.8.2")); Reject(() => DocsInputs.ReadManifest(temporary)); });
    Check("ManifestRejectsMissingFramework", () => { Manifest(current + "," + release.Replace(",\"framework\":\"net8.0\"", "")); Reject(() => DocsInputs.ReadManifest(temporary)); });
}
finally { File.Delete(temporary); }
Console.WriteLine($"Passed: {checks}, Failed: 0");
