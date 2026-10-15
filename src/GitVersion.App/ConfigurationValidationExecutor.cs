using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using SharpYaml;

namespace GitVersion;

internal sealed class ConfigurationValidationExecutor(
    IFileSystem fileSystem,
    IConsole console,
    IConfigurationFileLocator locator,
    ConfigurationResolver resolver)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public int Execute(GitVersionOptions options)
    {
        var info = options.ConfigurationInfo;
        var diagnostics = new List<Diagnostic>();
        string? file = null;
        string? version = null;
        try
        {
            version = ConfigurationVersionSelector.Resolve() == ConfigurationVersion.V6 ? "v6" : "v7";
            if (info.ValidationError is not null)
            {
                throw new ConfigurationValidationException("GVCONFIG_INVOCATION", "", info.ValidationError);
            }
            if (!fileSystem.Directory.Exists(options.WorkingDirectory))
            {
                throw new ConfigurationValidationException("GVCONFIG_FILE", "", "The selected directory does not exist.");
            }

            if (info.ConfigurationFile is { } explicitFile)
            {
                file = fileSystem.Path.GetFullPath(explicitFile, options.WorkingDirectory);
            }
            else
            {
                var root = fileSystem.FindGitDir(options.WorkingDirectory)?.WorkingTreeDirectory;
                if (root is not null)
                {
                    locator.Verify(options.WorkingDirectory, root);
                }
                file = locator.GetConfigurationFile(options.WorkingDirectory) ?? locator.GetConfigurationFile(root);
            }
            if (file is null || !fileSystem.File.Exists(file))
            {
                throw new ConfigurationValidationException("GVCONFIG_FILE", "", "Configuration file not found. Specify an existing file with --config.");
            }

            var content = fileSystem.File.ReadAllText(file);
            var parser = new OverrideConfigurationOptionParser();
            foreach (var value in info.ValidationOverrides)
            {
                try
                {
                    parser.SetValues([value], "--override-config");
                }
                catch (WarningException)
                {
                    var separator = value.IndexOf('=');
                    var path = separator > 0 ? value[..separator] : "";
                    throw new ConfigurationValidationException("GVCONFIG_OVERRIDE", path, "Invalid --override-config. Use a supported public property path and key=value syntax.");
                }
            }
            resolver.Resolve(content, parser.GetOverrideConfiguration());
            if (ConfigurationVersionSelector.IsExplicitV6())
            {
                diagnostics.Add(new("GVCONFIG_V6_COMPATIBILITY", "warning", "", "Temporary v6 compatibility mode is removed in GitVersion 7.1. Run 'gitversion config migrate' and validate with GITVERSION_CONFIGURATION_VERSION=v7."));
            }
        }
        catch (ConfigurationValidationException exception)
        {
            diagnostics.Add(new(exception.Code, "error", exception.PropertyPath, exception.Message));
        }
        catch (YamlException exception)
        {
            diagnostics.Add(new("GVCONFIG_YAML", "error", "", "Invalid YAML syntax or configuration value.", exception.Start.Line + 1, exception.Start.Column + 1));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(new("GVCONFIG_FILE", "error", "", "Configuration file could not be read."));
        }
        catch (ConfigurationException exception)
        {
            diagnostics.Add(new("GVCONFIG_LAYOUT", "error", "", exception.Message));
        }
        catch (WarningException)
        {
            diagnostics.Add(new("GVCONFIG_DISCOVERY", "error", "", "Ambiguous configuration file selection. Select a file with --config."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException)
        {
            diagnostics.Add(new("GVCONFIG_VALUE", "error", "", "Invalid configuration value or invocation."));
        }

        var valid = diagnostics.All(diagnostic => diagnostic.Severity != "error");
        foreach (var diagnostic in diagnostics)
        {
            var location = diagnostic.Line is { } line ? $"({line},{diagnostic.Column})" : "";
            Console.Error.WriteLine($"{file}{location}: {diagnostic.Severity} {diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}");
        }

        Console.Error.WriteLine($"{(valid ? "Configuration is valid." : "Configuration is invalid.")} {file}");
        if (info.ValidationJson)
        {
            console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, valid, configurationVersion = version, file, diagnostics }, JsonOptions));
        }

        return valid ? 0 : 1;
    }

    private sealed record Diagnostic(string Code, string Severity, string Path, string Message, int? Line = null, int? Column = null);
}
