Task("Clean-Documentation")
    .Does(() =>
{
    EnsureDirectoryExists(MakeAbsolute(Directory("artifacts/temp/_PublishedDocumentation")));
});

Task("Preview-Documentation")
    .WithCriteria(() => DirectoryExists(MakeAbsolute(Directory("docs"))), "Wyam documentation directory is missing")
    .Does<BuildParameters>((parameters) =>
{
    Wyam(new WyamSettings
    {
        Recipe = "Docs",
        Theme = "Samson",
        OutputPath = MakeAbsolute(MakeAbsolute(Directory("artifacts/Documentation"))),
        RootPath = MakeAbsolute(Directory("docs")),
        Preview = true,
        Watch = true,
        ConfigurationFile = MakeAbsolute((FilePath)"config.wyam"),
        PreviewVirtualDirectory = "GitVersion",
        Settings = new Dictionary<string, object>
        {
            { "Host",  "gittools.github.io" },
            { "LinkRoot",  "GitVersion" },
            { "BaseEditUrl", "https://github.com/gittools/GitVersion/tree/master/docs/input/" },
            { "SourceFiles", parameters.Paths.Directories.Source + "/**/{!bin,!obj,!packages,!*.Tests,}/**/*.cs" },
            { "Title", "GitVersion" },
            { "IncludeGlobalNamespace", false }
        }
    });
});
