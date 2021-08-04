using System.Collections.Generic;
using System.Linq;
using Common.Utilities;

namespace Artifacts.Utilities
{
    public static class Extensions
    {
        public static IEnumerable<string> GetDockerTagsForRepository(this BuildContext context, DockerImage dockerImage, string repositoryName)
        {
            var name = $"gittools/gitversion";
            var (distro, targetFramework) = dockerImage;

            if (context.Version == null) return Enumerable.Empty<string>();
            var tags = new List<string>
            {
                $"{name}:{context.Version.Version}-{distro}-{targetFramework}",
                $"{name}:{context.Version.SemVersion}-{distro}-{targetFramework}",
            };

            if (distro == Constants.DockerDistroLatest && targetFramework == Constants.Version50)
            {
                tags.AddRange(new[]
                {
                    $"{name}:{context.Version.Version}",
                    $"{name}:{context.Version.SemVersion}",

                    $"{name}:{context.Version.Version}-{distro}",
                    $"{name}:{context.Version.SemVersion}-{distro}"
                });

                if (context.IsStableRelease)
                {
                    tags.AddRange(new[]
                    {
                        $"{name}:latest",
                        $"{name}:latest-{targetFramework}",
                        $"{name}:latest-{distro}",
                        $"{name}:latest-{distro}-{targetFramework}",
                    });
                }
            }

            return tags;
        }
    }
}
