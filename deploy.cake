
Task("Publish-MyGet")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-MyGet Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-NuGet")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-Chocolatey")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-Chocolatey Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-Gem")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-Gem Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-GitHub-Release")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-GitHub-Release Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Deploy")
  .IsDependentOn("Publish-MyGet")
  .IsDependentOn("Publish-NuGet")
  .IsDependentOn("Publish-Chocolatey")
  .IsDependentOn("Publish-Gem")
  .IsDependentOn("Publish-GitHub-Release")
  .Finally(() =>
{
    if(publishingError)
    {
        throw new Exception("An error occurred during the publishing of Cake.  All publishing tasks have been attempted.");
    }
});