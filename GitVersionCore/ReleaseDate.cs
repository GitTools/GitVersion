using System;
using GitVersion;

public class ReleaseDate : IEquatable<ReleaseDate>
{
    public DateTimeOffset OriginalDate;
    public string OriginalCommitSha;
    public DateTimeOffset Date;
    public string CommitSha;

    private static readonly LambdaEqualityHelper<ReleaseDate> equalityHelper =
           new LambdaEqualityHelper<ReleaseDate>(x => x.OriginalDate, x => x.OriginalCommitSha, x => x.Date, x => x.CommitSha);

    public override bool Equals(object obj)
    {
        return Equals(obj as ReleaseDate);
    }

    public bool Equals(ReleaseDate other)
    {
        return equalityHelper.Equals(this, other);
    }

    public override int GetHashCode()
    {
        return equalityHelper.GetHashCode(this);
    }

    public static bool operator ==(ReleaseDate left, ReleaseDate right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ReleaseDate left, ReleaseDate right)
    {
        return !Equals(left, right);
    }
}
