Task("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Information("Cleaning directories..");

    CleanDirectories("./src/**/bin/" + parameters.Configuration);
    CleanDirectories("./src/**/obj");

    CleanDirectories(parameters.Paths.Directories.ToClean);
});

Task("Build")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Build(parameters);

    RunGitVersionOnCI(parameters);
});

Task("Validate-Version")
    .IsDependentOn("Build")
    .Does<BuildParameters>((parameters) =>
{
    ValidateVersion(parameters);
});
