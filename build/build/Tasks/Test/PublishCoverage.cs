using Cake.Codecov;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(PublishCoverage))]
[TaskDescription("Publishes the test coverage")]
[IsDependentOn(typeof(UnitTest))]
public class PublishCoverage : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsOnWindows, $"{nameof(PublishCoverage)} works only on Windows agents.");
        shouldRun &= context.ShouldRun(context.IsOnMainOrSupportBranchOriginalRepo, $"{nameof(PublishCoverage)} works only for on main or release branch original repository.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        var coverageFiles = context
            .GetFiles($"{Paths.TestOutput}/*.coverage.*.xml")
            .Select(file =>  context.MakeRelative(file).ToString()).ToArray();

        var token = context.Credentials?.CodeCov?.Token;
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Could not resolve CodeCov token.");
        }

        context.Codecov(new CodecovSettings
        {
            Files = coverageFiles,
            Token = token
        });
    }
}
