namespace Build.Utilities;

public static class ContextExtensions
{
    public static FilePath? GetGitVersionToolLocation(this BuildContext context) =>
        context.GetFiles($"src/GitVersion.App/bin/{context.MsBuildConfiguration}/{Constants.NetVersion60}/gitversion.dll").SingleOrDefault();
}
