using System.CommandLine;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Json.Schema;

namespace GitVersion.App.Tests;

[TestFixture]
public class CliSchemaExporterTests
{
    [Test]
    public void ExportsLiveTreeIncludingHiddenSymbolsAndLocalOptions()
    {
        var root = new RootCommand("Root description");
        var path = new Argument<string>("path") { Description = "Path description", Arity = ArgumentArity.OneOrMore, Hidden = true };
        var option = new Option<OutputType[]>("--output", "-o", "-z")
        {
            Description = "Output description",
            Required = true,
            Recursive = true,
            Hidden = true,
            HelpName = "format",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true
        };
        var child = new Command("child", "Child description") { Hidden = true };
        child.Aliases.Add("alias");
        child.Options.Add(new Option<string>("--output") { Arity = ArgumentArity.ExactlyOne });
        root.Arguments.Add(path);
        root.Options.Add(option);
        root.Subcommands.Add(child);

        var json = new CliSchemaExporter().Export(root);

        ValidateSchema(json);
        using var document = JsonDocument.Parse(json);
        var command = document.RootElement.GetProperty("command");
        command.GetProperty("name").GetString().ShouldBe("gitversion");
        command.GetProperty("description").GetString().ShouldBe("Root description");
        var argument = command.GetProperty("arguments")[0];
        argument.GetProperty("hidden").GetBoolean().ShouldBeTrue();
        argument.GetProperty("required").GetBoolean().ShouldBeTrue();
        argument.GetProperty("arity").GetProperty("minimum").GetInt32().ShouldBe(1);
        argument.GetProperty("arity").TryGetProperty("maximum", out _).ShouldBeFalse();
        var output = FindOption(command, "--output");
        output.GetProperty("aliases").EnumerateArray().Select(item => item.GetString()).ShouldBe(["-o", "-z"]);
        output.GetProperty("required").GetBoolean().ShouldBeTrue();
        output.GetProperty("recursive").GetBoolean().ShouldBeTrue();
        output.GetProperty("hidden").GetBoolean().ShouldBeTrue();
        output.GetProperty("arguments")[0].GetProperty("name").GetString().ShouldBe("format");
        output.GetProperty("arguments")[0].GetProperty("acceptedValues").EnumerateArray()
            .Select(item => item.GetString()).ShouldBe(Enum.GetNames<OutputType>());
        var valueMetadata = output.GetProperty("arguments")[0].GetProperty("metadata").EnumerateArray()
            .ToDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("value").GetBoolean());
        valueMetadata["gitversion.caseInsensitiveValues"].ShouldBeTrue();
        valueMetadata["gitversion.acceptedValuesExhaustive"].ShouldBeFalse();
        valueMetadata["gitversion.acceptsNumericValues"].ShouldBeTrue();
        var exportedChild = command.GetProperty("commands")[0];
        exportedChild.GetProperty("name").GetString().ShouldBe("child");
        exportedChild.GetProperty("aliases")[0].GetString().ShouldBe("alias");
        exportedChild.GetProperty("hidden").GetBoolean().ShouldBeTrue();
        exportedChild.GetProperty("options").GetArrayLength().ShouldBe(1);
        var childArgument = FindOption(exportedChild, "--output").GetProperty("arguments")[0];
        childArgument.GetProperty("arity").GetProperty("maximum").GetInt32().ShouldBe(1);
        childArgument.TryGetProperty("acceptedValues", out _).ShouldBeFalse();
    }

    [Test]
    public void DoesNotRunDefaultsValidatorsParsersOrCompletionSources()
    {
        var argument = new Argument<string>("path")
        {
            DefaultValueFactory = _ => throw new InvalidOperationException("default invoked"),
            CustomParser = _ => throw new InvalidOperationException("parser invoked")
        };
        argument.Validators.Add(_ => throw new InvalidOperationException("validator invoked"));
        argument.CompletionSources.Add(_ => throw new InvalidOperationException("completion invoked"));
        var root = new RootCommand { argument };

        var json = new CliSchemaExporter().Export(root);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("command").GetProperty("arguments")[0]
            .GetProperty("required").GetBoolean().ShouldBeFalse();
        json.ShouldNotContain("default invoked");
    }

    [Test]
    [NonParallelizable]
    public void OutputIsStableAcrossCulturesAndRepeatedExports()
    {
        var root = new RootCommand { new Option<bool>("--toggle"), new Option<bool>("--flag") { Arity = ArgumentArity.Zero } };
        var exporter = new CliSchemaExporter();
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var first = exporter.Export(root);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            exporter.Export(root).ShouldBe(first);
            exporter.Export(root).ShouldBe(first);
            using var document = JsonDocument.Parse(first);
            var command = document.RootElement.GetProperty("command");
            FindOption(command, "--flag").GetProperty("arguments").GetArrayLength().ShouldBe(0);
            var toggle = FindOption(command, "--toggle").GetProperty("arguments")[0];
            toggle.GetProperty("arity").GetProperty("minimum").GetInt32().ShouldBe(0);
            toggle.GetProperty("arity").GetProperty("maximum").GetInt32().ShouldBe(1);
            toggle.GetProperty("acceptedValues").EnumerateArray().Select(value => value.GetString()).ShouldBe(["true", "false"]);
            document.RootElement.GetProperty("info").GetProperty("version").GetString().ShouldBe(
                typeof(ArgumentParser).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestCase(0, 0)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(2, 4)]
    [TestCase(0, 100000)]
    [TestCase(1, 100000)]
    public void PreservesFiniteAndUnboundedArgumentArity(int minimum, int maximum)
    {
        var root = new RootCommand { new Argument<string[]>("values") { Arity = new(minimum, maximum) } };

        using var document = JsonDocument.Parse(new CliSchemaExporter().Export(root));

        var argument = document.RootElement.GetProperty("command").GetProperty("arguments")[0];
        argument.GetProperty("required").GetBoolean().ShouldBe(minimum > 0);
        var arity = argument.GetProperty("arity");
        arity.GetProperty("minimum").GetInt32().ShouldBe(minimum);
        if (maximum == 100000)
        {
            arity.TryGetProperty("maximum", out _).ShouldBeFalse();
        }
        else
        {
            arity.GetProperty("maximum").GetInt32().ShouldBe(maximum);
        }
    }

    internal static JsonElement FindOption(JsonElement command, string name) =>
        command.GetProperty("options").EnumerateArray().Single(option => option.GetProperty("name").GetString() == name);

    internal static void ValidateSchema(string json)
    {
        var schema = JsonSchema.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "OpenCli", "schema.json"),
            new BuildOptions { SchemaRegistry = new SchemaRegistry() });
        using var document = JsonDocument.Parse(json);
        var result = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        result.IsValid.ShouldBeTrue($"OpenCLI schema validation failed: {string.Join(", ", result.Details?.SelectMany(detail => detail.Errors?.Values.AsEnumerable() ?? []) ?? [])}");
    }
}
