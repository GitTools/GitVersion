namespace GitVersion;

/// <summary>Specifies the format in which GitVersion outputs version variables.</summary>
public enum OutputType
{
    /// <summary>Writes version variables to the CI build server's environment (e.g. TeamCity build parameters, GitHub Actions outputs).</summary>
    BuildServer,

    /// <summary>Writes version variables as a JSON object to standard output.</summary>
    Json,

    /// <summary>Writes version variables to a file on disk.</summary>
    File,

    /// <summary>Writes version variables in the <c>KEY=value</c> dotenv format.</summary>
    DotEnv
}
