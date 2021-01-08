using LibGit2Sharp;
using Tag = GitVersion.Tag;

namespace GitVersionCore.Tests.Mocks
{
    public class MockTag : Tag
    {
        public string NameEx;
        public override string FriendlyName => NameEx;

        public TagAnnotation AnnotationEx;

        public MockTag() { }

        public MockTag(string name)
        {
            NameEx = name;
        }
    }
}
