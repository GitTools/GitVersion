using System.Linq;
using Cake.Common.IO;
using Cake.Core.IO;
using Common.Utilities;

namespace Build.Utilities;

public static class ContextExtensions
{
    public static FilePath? GetGitVersionToolLocation(this BuildContext context) =>
        context.GetFiles($"src/GitVersion.App/bin/{context.MsBuildConfiguration}/{Constants.NetVersion50}/gitversion.dll").SingleOrDefault();
}