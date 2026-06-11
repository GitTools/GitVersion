namespace GitVersion.VersionCalculation;

/// <summary>Specifies the deployment strategy used to determine how the version number changes between releases.</summary>
public enum DeploymentMode
{
    /// <summary>Each build on a pre-release branch produces a unique pre-release version; the version is only finalised on an explicit release.</summary>
    ManualDeployment,

    /// <summary>Each commit is a potential release candidate; the build number is appended as the pre-release number.</summary>
    ContinuousDelivery,

    /// <summary>Each commit is automatically deployed; versions are calculated as if every commit is a new release.</summary>
    ContinuousDeployment
}
