using Cake.Common;
using Cake.Core;
using Common.Utilities;

namespace Publish.Utilities
{
    public class BuildCredentials
    {
        public GitterCredentials? Gitter { get; private set; }
        public NugetCredentials? Nuget { get; private set; }
        public ChocolateyCredentials? Chocolatey { get; private set; }

        public static BuildCredentials GetCredentials(ICakeContext context) => new()
        {
            Gitter = new GitterCredentials(
                context.EnvironmentVariable("GITTER_TOKEN"),
                context.EnvironmentVariable("GITTER_ROOM_ID")),

            Nuget = new NugetCredentials(
                context.EnvironmentVariable("NUGET_API_KEY"),
                context.EnvironmentVariable("NUGET_API_URL")),

            Chocolatey = new ChocolateyCredentials(
                context.EnvironmentVariable("CHOCOLATEY_API_KEY"),
                context.EnvironmentVariable("CHOCOLATEY_API_URL")),
        };
    }
}
