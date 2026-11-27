using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineIncrementStateTests
{
    [Test]
    public void ProjectionKeepsPendingOperatorsAndTheRealOperandBaseline()
    {
        var state = new MainlineIncrementState();
        var baseline = SemanticVersion.Parse("1.0.0-beta.5", null);
        state.Add(new BaseVersionOperand("tag", baseline));
        var operation = new BaseVersionOperator
        {
            Increment = VersionField.Minor,
            Label = "beta",
            ForceIncrement = true
        };

        state.Add(operation);

        state.ReleaseBaseline.ShouldBeSameAs(baseline);
        state.Version.ShouldNotBeNull().SemanticVersion.ShouldBeSameAs(baseline);
        state.Version.Operator.ShouldBeSameAs(operation);
        state.Version.GetIncrementedVersion().ToString().ShouldBe("1.1.0-beta.1");

        var resolved = new BaseVersion("resolved", new SemanticVersion(3, 0, 0));
        state.Add(resolved);

        state.Version.ShouldBeSameAs(resolved);
        state.ReleaseBaseline.ShouldBeSameAs(baseline);
        var nextRelease = SemanticVersion.Parse("4.0.0-beta.7", null);
        state.Add(new BaseVersionOperand("new tag", nextRelease));
        state.ReleaseBaseline.ShouldBeSameAs(nextRelease);
        state.Version.ShouldNotBeNull().GetIncrementedVersion().ToString().ShouldBe("4.0.0-beta.7");
        state.Version.Operator.ShouldBeNull();
    }

    [Test]
    public void FirstOperatorUsesTheEmptyVersionWithoutInventingAReleaseBaseline()
    {
        var state = new MainlineIncrementState();
        state.Add(new BaseVersionOperator { Increment = VersionField.Patch });

        state.ReleaseBaseline.ShouldBeNull();
        state.Version.ShouldNotBeNull().SemanticVersion.ShouldBe(SemanticVersion.Empty);
        state.Version.GetIncrementedVersion().ToString().ShouldBe("0.0.1-1");
    }
}
