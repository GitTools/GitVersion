using Common.Utilities;

namespace Build.Utilities;

public class Credentials
{
    public CodeCovCredentials? CodeCov { get; private init; }

    public static Credentials GetCredentials(ICakeContext context) => new()
    {
        CodeCov = new(context.EnvironmentVariable("CODECOV_TOKEN")),
    };
}
