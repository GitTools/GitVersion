namespace GitVersion.App.Tests;

[TestFixture]
public class ConfigurationValidationJsonTests
{
    [Test]
    public void ValidationEnvelopePreservesFieldNamesNullsAndDiagnosticDetails()
    {
        var result = new ConfigurationValidationResult(1, false, null, null,
            [new("GVCONFIG_FILE", "error", "", "file \"missing\"\n", null, null)]);

        var json = JsonSerializer.Serialize(result, ConfigurationValidationJsonContext.Default.ConfigurationValidationResult);

        json.ShouldBe("{\"schemaVersion\":1,\"valid\":false,\"configurationVersion\":null,\"file\":null,\"diagnostics\":[{\"code\":\"GVCONFIG_FILE\",\"severity\":\"error\",\"path\":\"\",\"message\":\"file \\u0022missing\\u0022\\n\",\"line\":null,\"column\":null}]}");
    }

    [Test]
    public void SuccessfulValidationPreservesEmptyDiagnostics()
    {
        var result = new ConfigurationValidationResult(1, true, "v7", "/repo/candidate.yml", []);

        var json = JsonSerializer.Serialize(result, ConfigurationValidationJsonContext.Default.ConfigurationValidationResult);

        json.ShouldBe("{\"schemaVersion\":1,\"valid\":true,\"configurationVersion\":\"v7\",\"file\":\"/repo/candidate.yml\",\"diagnostics\":[]}");
    }

    [Test]
    public void YamlDiagnosticPreservesNumericLocationAndEscapedUnicodePath()
    {
        var result = new ConfigurationValidationResult(1, false, "v7", "/repo/config é \"quoted\".yml",
            [new("GVCONFIG_YAML", "error", "calculation", "Invalid YAML.", 12, 34)]);

        var json = JsonSerializer.Serialize(result, ConfigurationValidationJsonContext.Default.ConfigurationValidationResult);

        json.ShouldBe("{\"schemaVersion\":1,\"valid\":false,\"configurationVersion\":\"v7\",\"file\":\"/repo/config \\u00E9 \\u0022quoted\\u0022.yml\",\"diagnostics\":[{\"code\":\"GVCONFIG_YAML\",\"severity\":\"error\",\"path\":\"calculation\",\"message\":\"Invalid YAML.\",\"line\":12,\"column\":34}]}");
    }
}
