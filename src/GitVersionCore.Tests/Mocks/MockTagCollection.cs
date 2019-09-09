using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    public class MockTagCollection : TagCollection, ICollection<Tag>
    {

        public List<Tag> Tags = new List<Tag>();
        public override IEnumerator<Tag> GetEnumerator()
        {
            return Tags.GetEnumerator();
        }

        IEnumerator<Tag> IEnumerable<Tag>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Tag item)
        {
            Tags.Add(item);
        }

        public void Clear()
        {
            Tags.Clear();
        }

        public bool Contains(Tag item)
        {
            return Tags.Contains(item);
        }

        public void CopyTo(Tag[] array, int arrayIndex)
        {
            Tags.CopyTo(array, arrayIndex);
        }

        public override void Remove(Tag tag)
        {
            Tags.Remove(tag);
        }

        bool ICollection<Tag>.Remove(Tag item)
        {
            return Tags.Remove(item);
        }

        public int Count => Tags.Count;
        public bool IsReadOnly => false;
    }
}