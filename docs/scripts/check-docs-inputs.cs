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

var packagesRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
try
{
    var legacy = Path.Combine(packagesRoot, "legacy");
    var legacyNet8 = Path.Combine(legacy, "tools", "net8.0", "any");
    var legacyNet9 = Path.Combine(legacy, "tools", "net9.0", "any");
    Directory.CreateDirectory(legacyNet8);
    Directory.CreateDirectory(legacyNet9);
    Check("LegacyToolDoesNotRequireRuntimePackage", () => Equal("none", DocsInputs.GetRuntimePackage(legacy, "linux-x64") ?? "none"));
    Check("LegacyToolUsesNewestAnyBundle", () =>
    {
        var bundle = DocsInputs.GetToolBundle(legacy, "6.8.2");
        Equal("net9.0", bundle.Framework);
        Equal(legacyNet9, bundle.Bundle);
    });
    Check("LegacyToolHonorsFrameworkOverride", () =>
    {
        var bundle = DocsInputs.GetToolBundle(legacy, "6.8.2", "net8.0");
        Equal("net8.0", bundle.Framework);
        Equal(legacyNet8, bundle.Bundle);
    });

    var parent = Path.Combine(packagesRoot, "parent");
    var settings = Path.Combine(parent, "tools", "any", "any", "DotnetToolSettings.xml");
    Directory.CreateDirectory(Path.GetDirectoryName(settings)!);
    File.WriteAllText(settings, """
        <DotNetCliTool Version="2">
          <RuntimeIdentifierPackages>
            <RuntimeIdentifierPackage RuntimeIdentifier="win-x64" Id="GitVersion.Tool.win-x64" />
            <RuntimeIdentifierPackage RuntimeIdentifier="linux-x64" Id="GitVersion.Tool.linux-x64" />
          </RuntimeIdentifierPackages>
        </DotNetCliTool>
        """);
    Check("RuntimePackageSelectsMatchingHost", () => Equal("GitVersion.Tool.linux-x64", DocsInputs.GetRuntimePackage(parent, "linux-x64")!));
    Check("RuntimePackageRejectsUnsupportedHost", () => Reject(() => DocsInputs.GetRuntimePackage(parent, "linux-arm64")));
    Check("ParentToolDoesNotContainFrameworkPayload", () => Reject(() => DocsInputs.GetToolBundle(parent, "7.0.0")));

    var child = Path.Combine(packagesRoot, "child");
    var childNet10 = Path.Combine(child, "tools", "net10.0", "linux-x64");
    var childNet11 = Path.Combine(child, "tools", "net11.0", "linux-x64");
    Directory.CreateDirectory(childNet10);
    Directory.CreateDirectory(childNet11);
    Directory.CreateDirectory(Path.Combine(child, "tools", "net11.0", "any"));
    Check("RuntimeToolUsesNewestRidBundle", () =>
    {
        var bundle = DocsInputs.GetToolBundle(child, "7.0.0", runtimeIdentifier: "linux-x64");
        Equal("net11.0", bundle.Framework);
        Equal(childNet11, bundle.Bundle);
    });
    Check("RuntimeToolHonorsFrameworkOverride", () =>
    {
        var bundle = DocsInputs.GetToolBundle(child, "7.0.0", "net10.0", "linux-x64");
        Equal("net10.0", bundle.Framework);
        Equal(childNet10, bundle.Bundle);
    });
    Check("RuntimeToolRejectsMissingRidBundle", () => Reject(() => DocsInputs.GetToolBundle(child, "7.0.0", runtimeIdentifier: "win-x64")));
    Check("RuntimeToolRejectsMissingFramework", () => Reject(() => DocsInputs.GetToolBundle(child, "7.0.0", "net12.0", "linux-x64")));
    File.WriteAllText(settings, """
        <DotNetCliTool Version="2">
          <RuntimeIdentifierPackages>
            <RuntimeIdentifierPackage RuntimeIdentifier="linux-x64" Id="" />
          </RuntimeIdentifierPackages>
        </DotNetCliTool>
        """);
    Check("RuntimePackageRejectsEmptyPackageId", () => Reject(() => DocsInputs.GetRuntimePackage(parent, "linux-x64")));
}
finally { Directory.Delete(packagesRoot, true); }
Console.WriteLine($"Passed: {checks}, Failed: 0");
