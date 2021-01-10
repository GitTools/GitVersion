using System.Collections.Generic;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    internal class MockTagCollection : TagCollection
    {
        private List<ITag> Tags = new List<ITag>();
        public override IEnumerator<ITag> GetEnumerator()
        {
            return Tags.GetEnumerator();
        }
        public void Add(ITag item)
        {
            Tags.Add(item);
        }
    }
}
