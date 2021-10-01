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
        shouldRun &= context.ShouldRun(context.IsOnMainBranchOriginalRepo, $"{nameof(PublishCoverage)} works only for on main branch original repository.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        var coverageFiles = context.GetFiles($"{Paths.TestOutput}/*.coverage.*.xml");

        var token = context.Credentials?.CodeCov?.Token;
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Could not resolve CodeCov token.");
        }

        foreach (var coverageFile in coverageFiles)
        {
            context.Codecov(new CodecovSettings
            {
                Files = new[] { coverageFile.ToString() },
                Token = token
            });
        }
    }
}
