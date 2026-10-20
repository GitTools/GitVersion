using GitVersion.Configuration.Attributes;

namespace GitVersion.Configuration.Tests;

[TestFixture]
public class JsonPropertyDefaultAttributeTests
{
    [Test]
    public void TypedDefaultsPreserveNullBooleanAndEnumRepresentations()
    {
        new JsonPropertyDefaultAttribute((string?)null).Value.ShouldBe("null");
        new JsonPropertyDefaultAttribute("quoted \"value\"").Value.ShouldBe("quoted \"value\"");
        new JsonPropertyDefaultAttribute(true).Value.ShouldBe("true");
        new JsonPropertyDefaultAttribute(false).Value.ShouldBe("false");
        new JsonPropertyDefaultAttribute(SemanticVersionFormat.Strict).Value.ShouldBe("Strict");
    }

    [Test]
    public void BoxedDefaultsPreserveScalarAndArbitraryObjectBehavior()
    {
        new JsonPropertyDefaultAttribute((object?)null).Value.ShouldBe("null");
        new JsonPropertyDefaultAttribute((object)42).Value.ShouldBe("42");
        new JsonPropertyDefaultAttribute((object)new DefaultValue { Number = 42 }).Value.ShouldBe("{\"Number\":42}");
        new JsonPropertyDefaultAttribute((object)new[] { 1, 2 }).Value.ShouldBe("[1,2]");
    }

    private sealed class DefaultValue
    {
        public int Number { get; init; }
    }
}
