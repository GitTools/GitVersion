using System;
using LibGit2Sharp;

public static class SignatureBuilder
{
    public static Signature ToSignature(this DateTimeOffset dateTimeOffset)
    {
        return new Signature("fakeName", "fakeEmail", dateTimeOffset);
    }

    public static Signature SignatureNow()
    {
        return new Signature("A. U. Thor", "thor@valhalla.asgard.com", DateTimeOffset.Now);
    }
}