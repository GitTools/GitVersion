using Cake.Common;
using Cake.Core;

namespace Build.Utilities
{
    public class BuildCredentials
    {
        public CodeCovCredentials? CodeCov { get; private set; }

        public static BuildCredentials GetCredentials(ICakeContext context)
        {
            return new()
            {
                CodeCov = CodeCovCredentials.GetCodeCovCredentials(context),
            };
        }
    }

    public record CodeCovCredentials(string Token)
    {
        public static CodeCovCredentials GetCodeCovCredentials(ICakeContext context)
        {
            return new(context.EnvironmentVariable("CODECOV_TOKEN"));
        }
    }
}
