namespace GitVersion.Configuration.Tests;

[TestFixture]
public class ConfigurationHelperTests
{
    [Test]
    public void Override_Replaces_Nested_Dictionary_With_Scalar_Value()
    {
        Dictionary<object, object?> original =
            new()
            {
                ["key"] = new Dictionary<object, object?> { ["nested"] = "value" }
            };
        var helper = new ConfigurationHelper(original);

        IReadOnlyDictionary<object, object?> source =
            new Dictionary<object, object?>
            {
                ["key"] = "override"
            };

        helper.Override(source);

        helper.Dictionary["key"].ShouldBe("override");
    }

    [Test]
    public void Override_Merges_Nested_Dictionaries_Recursively()
    {
        Dictionary<object, object?> original =
            new()
            {
                ["key"] = new Dictionary<object, object?>
                {
                    ["a"] = 1,
                    ["b"] = 2
                }
            };
        var helper = new ConfigurationHelper(original);

        IReadOnlyDictionary<object, object?> source =
            new Dictionary<object, object?>
            {
                ["key"] = new Dictionary<object, object?>
                {
                    ["b"] = 3,
                    ["c"] = 4
                }
            };

        helper.Override(source);

        var nested = (IDictionary<object, object?>)helper.Dictionary["key"]!;
        nested["a"].ShouldBe(1);
        nested["b"].ShouldBe(3);
        nested["c"].ShouldBe(4);
    }

    [Test]
    public void Override_Clones_New_Nested_Dictionaries()
    {
        Dictionary<object, object?> original = [];
        var helper = new ConfigurationHelper(original);

        Dictionary<object, object?> sourceNested =
            new()
            {
                ["a"] = 1
            };
        IReadOnlyDictionary<object, object?> source =
            new Dictionary<object, object?>
            {
                ["key"] = sourceNested
            };

        helper.Override(source);
        sourceNested["a"] = 2;

        var nested = (IDictionary<object, object?>)helper.Dictionary["key"]!;
        nested["a"].ShouldBe(1);
    }
}
