using Cake.Common;
using Cake.Core;
using Cake.Frosting;
using Common.Lifetime;
using Common.Utilities;

if (args.Length > 0)
{
    return new CakeHost().UseContext<ReleaseCheckContext>()
        .UseLifetime<BuildLifetimeBase<ReleaseCheckContext>>().UseRootDirectory().Run(args);
}

var sha = new string('a', 40);
var beta = ReleasePublication.Create("7.0.0-beta.1", sha);
Check("BetaPreservesTagAndFinalMilestone", beta.Name == "7.0.0-beta.1" && beta.Milestone == "7.0.0" && beta.Prerelease && !beta.CloseMilestone && beta.TargetCommitish == sha);
Check("BetaSelectsItsOwnCalculationLabel", beta.PrereleaseLabel == "beta");
var stable = ReleasePublication.Create("7.0.0", sha);
Check("StableClosesItsOwnMilestone", stable.Name == "7.0.0" && stable.Milestone == "7.0.0" && !stable.Prerelease && stable.CloseMilestone && stable.TargetCommitish == sha);
Check("StablePreservesDefaultCalculationLabel", stable.PrereleaseLabel is null);
Check("RcSelectsItsOwnCalculationLabel", ReleasePublication.Create("7.0.0-rc.2", sha).PrereleaseLabel == "rc");
Check("BetaDockerOmitsStableVersion", ReleasePublication.DockerVersionTags("7.0.0", "7.0.0-beta.1", true).SequenceEqual(["7.0.0-beta.1"]));
Check("StableDockerPreservesVersionAliases", ReleasePublication.DockerVersionTags("7.0.0", "7.0.0", false).SequenceEqual(["7.0.0"]));
Check("InternalDockerOmitsStableVersion", ReleasePublication.DockerVersionTags("7.0.0", "7.0.0-alpha.42", true).SequenceEqual(["7.0.0-alpha.42"]));
foreach (var (version, commit) in new[] { ("", sha), ("7.0.0", "") })
{
    try
    {
        ReleasePublication.Create(version, commit);
        throw new InvalidOperationException("Empty release identity was accepted");
    }
    catch (ArgumentException)
    {
        Console.WriteLine("PASS RejectsEmptyReleaseIdentity");
    }
}

return 0;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException(name);
    }
    Console.WriteLine($"PASS {name}");
}

public class ReleaseCheckContext(ICakeContext context) : BuildContextBase(context);

public class CheckReleaseIdentity : FrostingTask<ReleaseCheckContext>
{
    public override void Run(ReleaseCheckContext context)
    {
        var expected = context.EnvironmentVariable("GITVERSION_RELEASE_TAG");
        if (string.IsNullOrEmpty(expected) || context.Version?.SemVersion != expected ||
            !(expected.Contains('-') ? context.IsTaggedPreRelease : context.IsTaggedRelease))
        {
            throw new InvalidOperationException("Release lifetime did not preserve the selected release identity");
        }
        Console.WriteLine(expected.Contains('-') ? "PASS CakeLifetimePinsBetaVersionWhenMainAdvances" : "PASS CakeLifetimePinsStableVersionWhenMainAdvances");
    }
}
