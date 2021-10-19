namespace GitVersion.Core.Tests.Helpers;

public static class RuntimeHelper
{
#if !NETFRAMEWORK
    private static bool? _isCoreClr;
#endif

    public static bool IsCoreClr()
    {
#if !NETFRAMEWORK
        _isCoreClr ??= System.Environment.Version.Major >= 5
                       || System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Core");
        return _isCoreClr.Value;
#else
#pragma warning disable IDE0022 // Use expression body for methods // Cannot be set because of the pragma section
        return false;
#pragma warning restore IDE0022 // Use expression body for methods
#endif
    }
}
