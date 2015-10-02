using System;
using LibGit2Sharp;

public static class Constants
{
    static DateTimeOffset simulatedTime = DateTimeOffset.Now.AddHours(-1);

    public static DateTimeOffset Now
    {
        get
        {
            simulatedTime = simulatedTime.AddMinutes(1);
            return simulatedTime;
        }
    }

    public static Signature SignatureNow()
    {
        var dateTimeOffset = Now;
        return Signature(dateTimeOffset);
    }

    public static Signature Signature(DateTimeOffset dateTimeOffset)
    {
        return new Signature("A. U. Thor", "thor@valhalla.asgard.com", dateTimeOffset);
    }
}