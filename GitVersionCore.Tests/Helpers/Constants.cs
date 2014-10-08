using System;
using LibGit2Sharp;

public static class Constants
{
    public static Signature SignatureNow()
    {
        var dateTimeOffset = DateTimeOffset.Now;
        return Signature(dateTimeOffset);
    }

    public static Signature Signature(DateTimeOffset dateTimeOffset)
    {
        return new Signature("A. U. Thor", "thor@valhalla.asgard.com", dateTimeOffset);
    }
}