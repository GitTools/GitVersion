using System;
using System.Collections.Generic;

namespace GitVersion.VersionCalculation
{
    public class VersionStrategyBase : IVersionStrategy
    {
        protected readonly IGitVersionContextFactory ContextFactory;

        public VersionStrategyBase(IGitVersionContextFactory gitVersionContextFactory)
        {
            this.ContextFactory = gitVersionContextFactory ?? throw new ArgumentNullException(nameof(gitVersionContextFactory));
        }
        public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context) => throw new NotImplementedException();
    }
}
