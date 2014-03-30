using System;
using LibGit2Sharp;

public static class Constants
{
    public static Signature SignatureNow()
    {
        return new Signature("A. U. Thor", "thor@valhalla.asgard.com", DateTimeOffset.Now);
    }
}