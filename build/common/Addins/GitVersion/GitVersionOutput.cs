namespace Common.Addins.GitVersion;

/// <summary>
/// The Git version output type.
/// </summary>
public enum GitVersionOutput
{
    /// <summary>
    /// Outputs to the stdout using json.
    /// </summary>
    Json,

    /// <summary>
    /// Outputs to the stdout in a way usable by a detected build server.
    /// </summary>
    BuildServer
}
