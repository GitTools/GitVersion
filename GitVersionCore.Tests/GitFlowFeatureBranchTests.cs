using System;
using GitVersion;
using NUnit.Framework;

[TestFixture]
public class LastMinorVersionFinderTests
{
    [Test]
    public void Should_get_last_minor_release()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            var commit0 = fixture.Repository.MakeACommit(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero));
            fixture.Repository.Tags.Add("1.0.0", commit0);
            var commit1 = fixture.Repository.MakeACommit(new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero));
            fixture.Repository.Tags.Add("1.1.0", commit1);
            var commit2 = fixture.Repository.MakeACommit(new DateTimeOffset(2002, 1, 1, 1, 1, 1, TimeSpan.Zero));
            fixture.Repository.Tags.Add("1.1.1", commit2);
           
            var dateTimeOffset = LastMinorVersionFinder.Execute(fixture.Repository, new Config(), fixture.Repository.Head.Tip);
            Assert.AreEqual(2001, dateTimeOffset.Year);
        }
    }    
    [Test]
    public void Should_ignore_invalid_tag()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            var commit1 = fixture.Repository.MakeACommit(new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero));
            fixture.Repository.Tags.Add("1.1.0", commit1);
            var commit2 = fixture.Repository.MakeACommit(new DateTimeOffset(2002, 1, 1, 1, 1, 1, TimeSpan.Zero));
            fixture.Repository.Tags.Add("BadTag", commit2);
           
            var dateTimeOffset = LastMinorVersionFinder.Execute(fixture.Repository, new Config(), fixture.Repository.Head.Tip);
            Assert.AreEqual(2001, dateTimeOffset.Year);
        }
    }    
}