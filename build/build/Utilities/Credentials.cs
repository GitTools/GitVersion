using Cake.Common;
using Cake.Core;
using Common.Utilities;

namespace Build.Utilities
{
    public class BuildCredentials
    {
        public CodeCovCredentials? CodeCov { get; private set; }

        public static BuildCredentials GetCredentials(ICakeContext context) => new()
        {
            CodeCov = new CodeCovCredentials(context.EnvironmentVariable("CODECOV_TOKEN")),
        };
    }
}
