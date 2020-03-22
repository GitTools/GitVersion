using System;
using System.Collections.Generic;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionCalculation
{
    public class VersionStrategyBase : IVersionStrategy
    {
        protected readonly IRepository Repository;
        protected readonly GitVersionContext Context;

        protected VersionStrategyBase(IRepository repository, IOptions<GitVersionContext> versionContext)
        {
            Repository = repository;
            Context = versionContext.Value;
        }
        public virtual IEnumerable<BaseVersion> GetVersions()
        {
            throw new NotImplementedException();
        }
    }
}
