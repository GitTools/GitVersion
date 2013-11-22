using System.Collections.Generic;
using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class LibGitExtensionsTests
{
    [Test]
    public void NewestSemVerTag_RetrieveTheHighestSemanticVersionPointingAtTheSpecifiedCommit()
    {
        var mockCommit = new MockCommit();
        var repo = new MockRepository
            {
                Tags = new MockTagCollection
                    {
                        Tags = new List<Tag>
                            {
                                new MockTag
                                    {
                                        NameEx = "a",
                                        TargetEx = mockCommit,
                                        AnnotationEx = new MockTagAnnotation
                                            {
                                                TaggerEx = new Signature("a", "", 7.Seconds().Ago())
                                            }
                                    },
                                new MockTag
                                    {
                                        NameEx = "9.0.0a",
                                        TargetEx = mockCommit,
                                        AnnotationEx = new MockTagAnnotation
                                            {
                                                TaggerEx = new Signature("a", "", 5.Seconds().Ago())
                                            }
                                    },
                                new MockTag
                                    {
                                        NameEx = "0.1.0",
                                        TargetEx = mockCommit,
                                        AnnotationEx = new MockTagAnnotation
                                            {
                                                TaggerEx = new Signature("a", "", 1.Seconds().Ago())
                                            }
                                    },
                                new MockTag
                                    {
                                        NameEx = "0.2.0",
                                        TargetEx = mockCommit,
                                        AnnotationEx = new MockTagAnnotation
                                            {
                                                TaggerEx = new Signature("a", "", 5.Seconds().Ago())
                                            }
                                    },
                            }
                    }
            };

        var version = repo.NewestSemVerTag(mockCommit);

        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(0, version.Patch);
    }

    [Test]
    public void NewestSemVerTag_ReturnNullWhenNoTagPointingAtTheSpecifiedCommitHasBeenFound()
    {
        var tagNames = new[] { "a", "9.0.0", "z", "0.1.0", "11.1.0", "0.2.0" };

        var col = new MockTagCollection();
        foreach (var tagName in tagNames)
        {
            col.Add(new MockTag
            {
                NameEx = tagName,
                TargetEx = null,
                AnnotationEx = new MockTagAnnotation
                {
                    TaggerEx = new Signature("a", "", 5.Seconds().Ago())
                }
            });
        }

        var repo = new MockRepository { Tags = col };

        var version = repo.NewestSemVerTag(new MockCommit());

        Assert.IsNull(version);
    }
}
