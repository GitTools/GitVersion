namespace GitVersion;

/// <summary>Controls WiX installer version file update behaviour.</summary>
public record WixInfo
{
    /// <summary>Gets or sets a value indicating whether the WiX version file should be updated with the calculated version.</summary>
    public bool UpdateWixVersionFile;
}
