using System;

public static  class VersionConverter
{
    public static Version ToVersion(this string target)
    {
        return Version.Parse(target);
    }
}