using System;

namespace GitVersion.VersioningModes
{
    public static class VersioningModeExtension
    {
        public static VersioningModeBase GetInstance(this VersioningMode _this)
        {
            return _this switch
            {
                VersioningMode.ContinuousDelivery => new ContinuousDeliveryMode(),
                VersioningMode.ContinuousDeployment => new ContinuousDeploymentMode(),
                _ => throw new ArgumentException("No instance exists for this versioning mode.")
            };
        }
    }
}
