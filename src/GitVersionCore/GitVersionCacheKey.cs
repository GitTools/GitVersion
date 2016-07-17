namespace GitVersion
{
    using System;

    public class GitVersionCacheKey
    {
        public GitVersionCacheKey(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("value");
            }

            Value = value;
        }

        public string Value { get; private set; }
    }
}