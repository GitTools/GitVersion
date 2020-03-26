using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionCalculation
{
    public class VersionStrategyBase : IVersionStrategy
    {
        protected readonly GitVersionContext Context;

        protected VersionStrategyBase(IOptions<GitVersionContext> versionContext)
        {
            Context = versionContext.Value;
        }
        public virtual IEnumerable<BaseVersion> GetVersions()
        {
            throw new NotImplementedException();
        }
    }
}
