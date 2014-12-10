namespace GitVersion
{
    using System;

    using GitVersion.VersioningModes;

    public enum VersioningMode
    {
        ContinuousDelivery,
        ContinuousDeployment
    }

    public static class VersioningModeExtension
    {
        public static VersioningModeBase GetInstance(this VersioningMode _this)
        {
            switch (_this)
            {
                case VersioningMode.ContinuousDelivery:
                    return new ContinuousDeliveryMode();
                case VersioningMode.ContinuousDeployment:
                    return new ContinuousDeploymentMode();
                default:
                    throw new ArgumentException("No instance exists for this versioning mode.");
            }
        }
    }
}
