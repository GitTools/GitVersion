#nullable disable
namespace Common.Addins.Cake.Docker;

/// <summary>
/// Settings for docker buildx imagetools create.
/// </summary>
public sealed class DockerBuildXImageToolsCreateSettings : AutoToolSettings
{
    /// <summary>
    /// Append to existing manifest
    /// </summary>
    public bool Append { get; set; }
    /// <summary>
    /// Override the configured builder instance
    /// </summary>
    public string Builder { get; set; }
    /// <summary>
    /// Show final image instead of pushing
    /// </summary>
    public bool DryRun { get; set; }
    /// <summary>
    /// Read source descriptor from file
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] File { get; set; }
    /// <summary>
    /// Set reference for new image
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Tag { get; set; }
    /// <summary>
    /// Set annotation for new image
    /// </summary>
    [AutoProperty(AutoArrayType = AutoArrayType.List)]
    public string[] Annotation { get; set; }
}
