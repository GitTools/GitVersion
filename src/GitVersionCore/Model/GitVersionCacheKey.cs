using System;

namespace GitVersion.Cache
{
    public class GitVersionCacheKey
    {
        public GitVersionCacheKey(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            Value = value;
        }

        public string Value { get; private set; }
    }
}
