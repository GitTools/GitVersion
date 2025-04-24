using GitVersion.Configuration;
using GitVersion.OutputVariables;
using GitVersion.Schema;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.Extensions.Configuration;

var configurationManager = new ConfigurationManager().AddCommandLine(args).Build();
var schemasDirectory = configurationManager["OutputDirectory"]!;
var schemaVersion = configurationManager["Version"]!;

var configuration = new SchemaGeneratorConfiguration
{
    PropertyNameResolver = PropertyNameResolvers.KebabCase,
    PropertyOrder = PropertyOrder.ByName
};

AttributeHandler.AddHandler<DefaultAttributeHandler>();
AttributeHandler.AddHandler<DescriptionAttributeHandler1>();
AttributeHandler.AddHandler<DescriptionAttributeHandler2>();
AttributeHandler.AddHandler<FormatAttributeHandler>();

if (!Directory.Exists(schemasDirectory))
{
    Directory.CreateDirectory(schemasDirectory);
}
if (!Directory.Exists(Path.Combine(schemasDirectory, schemaVersion)))
{
    Directory.CreateDirectory(Path.Combine(schemasDirectory, schemaVersion));
}

var builder = new JsonSchemaBuilder();
builder.Schema("http://json-schema.org/draft-07/schema#");
builder.Id($"https://gitversion.net/schemas/{schemaVersion}/GitVersion.configuration.json");
builder.Title($"GitVersion Configuration ({schemaVersion})");
builder.Description($"GitVersion configuration schema ({schemaVersion})");
var schema = builder.FromType<GitVersionConfiguration>(configuration).Build();

var fileName = Path.Combine(schemasDirectory, schemaVersion, "GitVersion.configuration.json");
Console.WriteLine($"Writing schema to {fileName}");
schema.WriteToFile(fileName);

configuration.PropertyNameResolver = PropertyNameResolvers.AsDeclared;

builder = new();
builder.Schema("http://json-schema.org/draft-07/schema#");
builder.Id($"https://gitversion.net/schemas/{schemaVersion}/GitVersion.json");
builder.Title("GitVersion version variables output");
builder.Description("GitVersion output schema");
schema = builder.FromType<VersionVariablesJsonModel>(configuration).Build();

fileName = Path.Combine(schemasDirectory, schemaVersion, "GitVersion.json");
Console.WriteLine($"Writing schema to {fileName}");
schema.WriteToFile(fileName);
