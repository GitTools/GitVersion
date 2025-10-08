using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests;

[TestFixture]
public class YamlAotCompatibilityTests
{
    [Test]
    public void YamlConfigurationContextCanBeInstantiated()
    {
        // Arrange & Act
        var context = new YamlConfigurationContext();

        // Assert
        // The fact that we can instantiate the context means the infrastructure is in place
        // Once the source generator issues are resolved, this context will provide AOT-compatible serialization
        context.ShouldNotBeNull();
        context.ShouldBeOfType<YamlConfigurationContext>();
    }

    [Test]
    public void YamlConfigurationContextHasCorrectBaseType()
    {
        // Arrange
        var context = new YamlConfigurationContext();

        // Assert
        // Verify it inherits from StaticContext as required by YamlDotNet source generator
        context.ShouldBeAssignableTo<YamlDotNet.Serialization.StaticContext>();
    }

    /// <summary>
    /// Note: Full AOT serialization/deserialization tests are currently disabled due to known issues
    /// in the Vecc.YamlDotNet.Analyzers.StaticGenerator package (TypeFactoryGenerator failures).
    /// 
    /// When the source generator issues are resolved, the following functionality should work:
    /// - StaticSerializerBuilder(context).Build() for AOT-compatible serialization
    /// - StaticDeserializerBuilder(context).Build() for AOT-compatible deserialization
    /// 
    /// See: https://github.com/aaubry/YamlDotNet/issues/740
    /// </summary>
    [Test]
    [Ignore("Source generator has known issues - TypeFactoryGenerator fails with IndexOutOfRangeException")]
    public void StaticContextCanSerializeConfiguration()
    {
        // This test is placeholder for future when source generator is fixed
        var context = new YamlConfigurationContext();
        // var serializer = new StaticSerializerBuilder(context).Build();
        // Test serialization...
    }
}
