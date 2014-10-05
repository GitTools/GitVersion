using System.Linq;
using FluentDate;
using FluentDateTimeOffset;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class LibGitExtensionsTests
{

    [Test]
    public void TagsByDate_HonorChainedAnnotatedTags()
    {
        var c = new MockCommit();
        
        var col = new MockTagCollection();

        var first = new MockTag
                      {
                          NameEx = "first",
                          TargetEx = c,
                          AnnotationEx = new MockTagAnnotation
                                         {
                                             TaggerEx = new Signature("a", "", 5.Seconds().Ago()),
                                             TargetEx = c,
                                         }
                      };

        col.Add(first);

        col.Add(new MockTag
                {
                    NameEx = "second",
                    TargetEx = first.Annotation,
                    AnnotationEx = new MockTagAnnotation
                    {
                        TaggerEx = new Signature("a", "", 2.Seconds().Ago()),
                        TargetEx = c,
                    }

                });

        var repo = new MockRepository { Tags = col };

        var tags = repo.TagsByDate(c);

        Assert.AreEqual("second", tags.First().Name);
    }
}
