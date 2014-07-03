using LibGit2Sharp;
using NUnit.Framework;

public class LambdaEqualityHelperFixture
{
    // From the LibGit2Sharp project (libgit2sharp.com)
    // MIT License - Copyright (c) 2011-2014 LibGit2Sharp contributors
    // see https://github.com/libgit2/libgit2sharp/blob/7af5c60f22f9bd6064204f84467cfa62bedd1147/LibGit2Sharp.Tests/EqualityFixture.cs

    [Test]
    public void EqualityHelperCanTestNullInEquals()
    {
        var one = new ObjectWithEquality();
        var two = new ObjectWithEquality();
        var three = new ObjectWithEquality(ObjectId.Zero);
        var four = new ObjectWithEquality(ObjectId.Zero);

        Assert.True(one.Equals(one));
        Assert.True(two.Equals(two));
        Assert.True(three.Equals(four));
        Assert.True(four.Equals(three));
        Assert.False(one.Equals(three));
        Assert.False(three.Equals(one));
    }

    [Test]
    public void EqualityHelperCanTestNullInHashCode()
    {
        var one = new ObjectWithEquality();
        var two = new ObjectWithEquality();
        var three = new ObjectWithEquality(ObjectId.Zero);
        var four = new ObjectWithEquality(ObjectId.Zero);

        Assert.AreEqual(one.GetHashCode(), two.GetHashCode());
        Assert.AreEqual(three.GetHashCode(), four.GetHashCode());
        Assert.AreNotEqual(one.GetHashCode(), three.GetHashCode());
    }

    private class ObjectWithEquality : GitObject
    {
        ObjectId id;

        public ObjectWithEquality(ObjectId id = null)
        {
            this.id = id;
        }

        public override ObjectId Id
        {
            get { return id; }
        }
    }
}
