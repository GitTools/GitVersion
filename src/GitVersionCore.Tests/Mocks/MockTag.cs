using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    internal class MockTag : Tag
    {
        public string NameEx;
        public override string FriendlyName => NameEx;

        public MockTag() { }

        public MockTag(string name)
        {
            NameEx = name;
        }
    }
}
