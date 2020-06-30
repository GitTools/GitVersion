using System;
using System.Collections;
using Microsoft.Build.Framework;

namespace GitVersionTask.Tests.Mocks
{
    internal class MockTaskItem : ITaskItem
    {
        public string ItemSpec { get; set; }

        public int MetadataCount { get; private set; }

        public ICollection MetadataNames { get; private set; }

        public IDictionary CloneCustomMetadata()
        {
            throw new NotImplementedException();
        }

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
            throw new NotImplementedException();
        }

        public string GetMetadata(string metadataName)
        {
            throw new NotImplementedException();
        }

        public void RemoveMetadata(string metadataName)
        {
            throw new NotImplementedException();
        }

        public void SetMetadata(string metadataName, string metadataValue)
        {
            throw new NotImplementedException();
        }
    }
}
