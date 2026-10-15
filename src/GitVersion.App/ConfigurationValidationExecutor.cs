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
    private const string ErrorSeverity = "error";

    public int Execute(GitVersionOptions options)
    {
        var info = options.ConfigurationInfo;
        var diagnostics = new List<Diagnostic>();
        string? file = null;
        string? version = null;
        try
        {
            version = ConfigurationVersionSelector.ResolveName();
            ValidateInvocation(options);
            file = SelectFile(options);
            ValidateFile(file, info.ValidationOverrides);
            if (ConfigurationVersionSelector.IsExplicitV6())
            {
                diagnostics.Add(new("GVCONFIG_V6_COMPATIBILITY", "warning", "", "Temporary v6 compatibility mode is removed in GitVersion 7.1. Run 'gitversion config migrate' and validate with GITVERSION_CONFIGURATION_VERSION=v7."));
            }
        }
        catch (ConfigurationValidationException exception)
        {
            diagnostics.Add(new(exception.Code, ErrorSeverity, exception.PropertyPath, exception.Message));
        }
        catch (YamlException exception)
        {
            diagnostics.Add(new("GVCONFIG_YAML", ErrorSeverity, "", "Invalid YAML syntax or configuration value.", exception.Start.Line + 1, exception.Start.Column + 1));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(new("GVCONFIG_FILE", ErrorSeverity, "", "Configuration file could not be read."));
        }
        catch (ConfigurationException)
        {
            diagnostics.Add(new("GVCONFIG_LAYOUT", ErrorSeverity, "", "Invalid configuration layout. Check property placement and the selected configuration version; use 'gitversion config migrate' for a v6 file."));
        }
        catch (WarningException)
        {
            diagnostics.Add(new("GVCONFIG_DISCOVERY", ErrorSeverity, "", "Ambiguous configuration file selection. Select a file with --config."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException)
        {
            diagnostics.Add(new("GVCONFIG_VALUE", ErrorSeverity, "", "Invalid configuration value or invocation."));
        }

        return WriteResult(info.ValidationJson, version, file, diagnostics);
    }

    private void ValidateInvocation(GitVersionOptions options)
    {
        if (options.ConfigurationInfo.ValidationError is { } error)
        {
            throw new ConfigurationValidationException("GVCONFIG_INVOCATION", "", error);
        }
        if (!fileSystem.Directory.Exists(options.WorkingDirectory))
        {
            throw new ConfigurationValidationException("GVCONFIG_FILE", "", "The selected directory does not exist.");
        }
    }

    private string? SelectFile(GitVersionOptions options)
    {
        if (options.ConfigurationInfo.ConfigurationFile is { } explicitFile)
        {
            return fileSystem.Path.GetFullPath(explicitFile, options.WorkingDirectory);
        }

        var root = fileSystem.FindGitDir(options.WorkingDirectory)?.WorkingTreeDirectory;
        if (root is not null)
        {
            locator.Verify(options.WorkingDirectory, root);
        }
        return locator.GetConfigurationFile(options.WorkingDirectory) ?? locator.GetConfigurationFile(root);
    }

    private void ValidateFile(string? file, string[] overrides)
    {
        if (file is null || !fileSystem.File.Exists(file))
        {
            throw new ConfigurationValidationException("GVCONFIG_FILE", "", "Configuration file not found. Specify an existing file with --config.");
        }

        var content = fileSystem.File.ReadAllText(file);
        resolver.Resolve(content, ParseOverrides(overrides));
    }

    private static IReadOnlyDictionary<object, object?> ParseOverrides(string[] overrides)
    {
        var parser = new OverrideConfigurationOptionParser();
        foreach (var value in overrides)
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
        return parser.GetOverrideConfiguration();
    }

    private int WriteResult(bool json, string? version, string? file, List<Diagnostic> diagnostics)
    {
        var valid = diagnostics.All(diagnostic => diagnostic.Severity != ErrorSeverity);
        foreach (var diagnostic in diagnostics)
        {
            var location = diagnostic.Line is { } line ? $"({line},{diagnostic.Column})" : "";
            Console.Error.WriteLine($"{file}{location}: {diagnostic.Severity} {diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}");
        }

        Console.Error.WriteLine($"{(valid ? "Configuration is valid." : "Configuration is invalid.")} {file}");
        if (json)
        {
            console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, valid, configurationVersion = version, file, diagnostics }, JsonOptions));
        }

        return valid ? 0 : 1;
    }

    private sealed record Diagnostic(string Code, string Severity, string Path, string Message, int? Line = null, int? Column = null);
}
