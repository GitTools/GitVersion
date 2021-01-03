using System.Collections.Generic;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    public class MockTagCollection : TagCollection
    {
        private List<Tag> Tags = new List<Tag>();
        public override IEnumerator<Tag> GetEnumerator()
        {
            return Tags.GetEnumerator();
        }
        public void Add(Tag item)
        {
            Tags.Add(item);
        }
    }
}
