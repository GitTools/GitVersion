using System.Linq;
using Cake.Common.IO;
using Cake.Core.IO;

namespace Build.Utils
{
    public static class ContextExtensions
    {
        public static FilePath? GetGitVersionToolLocation(this BuildContext context)
        {
            return context.GetFiles($"src/GitVersion.App/bin/{context.MsBuildConfiguration}/{Constants.NetVersion50}/gitversion.dll").SingleOrDefault();
        }
    }
}
