using System;
using System.Collections.Generic;
using System.Text;

namespace GitVersion
{
    public static class GitVersionCoreDefaults
    {
        public const int LockTimeoutInMilliseconds = 1000 * 15;
        public const string LockFileNameWithExtensions = "GitVersion.lock";
    }
}
