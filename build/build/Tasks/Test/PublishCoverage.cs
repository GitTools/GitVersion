using System;
using Build.Utilities;
using Cake.Codecov;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(PublishCoverage))]
    [TaskDescription("Publishes the test coverage.")]
    [IsDependentOn(typeof(UnitTest))]
    public class PublishCoverage : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            if (context.IsOnWindows)
            {
                context.Information("PublishCoverage works only on Windows agents.");
                return true;
            }
            if (context.IsStableRelease() || context.IsPreRelease())
            {
                context.Information("PublishCoverage works only for releases.");
                return true;
            }
            return false;
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
}
