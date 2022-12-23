#nullable disable
namespace Common.Addins.Cake.Wyam;

/// <summary>
/// Contains settings used by <see cref="WyamRunner"/>.
/// </summary>
public sealed class WyamSettings : DotNetSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable watching of input folder for changes to files.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool Watch { get; set; }

    /// <summary>
    /// Pauses execution while waiting for a debugger to attach.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool Attach { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable previewing of the generated content in built in web server.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool Preview { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the port number to use for previewing.
    /// </summary>
    /// <remarks>Default is 5080</remarks>
    public int PreviewPort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable forcing of using file extensions.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool PreviewForceExtensions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the virtual directory to use for the preview server.
    /// </summary>
    public DirectoryPath PreviewVirtualDirectory { get; set; }

    /// <summary>
    /// The path to the root of the preview server, if not the output folder.
    /// </summary>
    public DirectoryPath PreviewRoot { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the input paths that should be used while running Wyam.
    /// </summary>
    public IEnumerable<DirectoryPath> InputPaths { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the output path that should be used while running Wyam.
    /// </summary>
    public DirectoryPath OutputPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the configuration file that should be used while running Wyam.
    /// </summary>
    public FilePath ConfigurationFile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable updating of packages.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool UpdatePackages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use a local NuGet packages folder.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool UseLocalPackages { get; set; }

    /// <summary>
    /// Toggles the use of the global NuGet sources.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool UseGlobalSources { get; set; }

    /// <summary>
    /// Ignores default NuGet sources like the NuGet Gallery.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool IgnoreDefaultSources { get; set; }

    /// <summary>
    /// Gets or sets the packages path to use.
    /// </summary>
    public DirectoryPath PackagesPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to output the script at end of execution.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool OutputScript { get; set; }

    /// <summary>
    /// Compile the configuration but do not execute.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool VerifyConfig { get; set; }

    /// <summary>
    /// Force evaluating the configuration file, even when no changes were detected.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool IgnoreConfigHash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prevent cleaning of the output path on each execution if <c>true</c>.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool NoClean { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to turn off the caching mechanism on all modules if <c>true</c>.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool NoCache { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to run in verbose mode.
    /// </summary>
    /// <remarks>Default is false</remarks>
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets metadata settings.
    /// </summary>
    public IDictionary<string, object> Settings { get; set; }

    /// <summary>
    /// Gets or sets the path to the Wyam log file.
    /// </summary>
    public FilePath LogFilePath { get; set; }

    /// <summary>
    /// Gets or sets the The folder (or config file) to use as the root.
    /// </summary>
    /// <remarks>Default is the current working directory</remarks>
    public DirectoryPath RootPath { get; set; }

    /// <summary>
    /// Adds NuGet packages (downloading and installing them if needed).
    /// </summary>
    public IEnumerable<NuGetSettings> NuGetPackages { get; set; }

    /// <summary>
    /// Specifies additional package sources to use.
    /// </summary>
    public IEnumerable<string> NuGetSources { get; set; }

    /// <summary>
    /// Adds references to multiple assemblies by name, file name, or globbing patterns.
    /// </summary>
    public IEnumerable<string> Assemblies { get; set; }

    /// <summary>
    /// Gets or sets the recipe.
    /// </summary>
    public string Recipe { get; set; }

    /// <summary>
    /// Gets or sets the theme.
    /// </summary>
    public string Theme { get; set; }

    /// <summary>
    /// Specifies additional supported content types for the preview server.
    /// </summary>
    public IDictionary<string, string> ContentTypes { get; set; }
}
