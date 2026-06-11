namespace GitVersion.Git;

/// <summary>Indicates the direction of a refspec mapping.</summary>
public enum RefSpecDirection
{
    /// <summary>The refspec is used when fetching from a remote.</summary>
    Fetch,

    /// <summary>The refspec is used when pushing to a remote.</summary>
    Push
}
