using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Cache;
using GitVersion.VersionConverters.AssemblyInfo;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersion.VersionConverters.OutputGenerator;
using GitVersion.VersionConverters.WixUpdater;
using System;

namespace GitVersion.Cli
{
    /// <summary>
    /// A new implementation of IGitVersionCalculator that doesn't depend on IOptions<GitVersionOptions>
    /// </summary>
    public class GitVersionCalculator : IGitVersionCalculator
    {
        private readonly ILog log;
        private readonly IGitVersionCache gitVersionCache;
        private readonly INextVersionCalculator nextVersionCalculator;
        private readonly IVariableProvider variableProvider;
       // private readonly IGitPreparer gitPreparer;
        private readonly IGitVersionCacheKeyFactory cacheKeyFactory;

        private readonly Lazy<GitVersionContext> versionContext;
        private GitVersionContext context => versionContext.Value;

        public GitVersionCalculator(ILog log, INextVersionCalculator nextVersionCalculator, IVariableProvider variableProvider,
           // IGitPreparer gitPreparer,
            IGitVersionCache gitVersionCache, IGitVersionCacheKeyFactory cacheKeyFactory,
            IOutputGenerator outputGenerator, IWixVersionFileUpdater wixVersionFileUpdater, IGitVersionInfoGenerator gitVersionInfoGenerator, IAssemblyInfoFileUpdater assemblyInfoFileUpdater,
            Lazy<GitVersionContext> versionContext)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            this.nextVersionCalculator = nextVersionCalculator ?? throw new ArgumentNullException(nameof(nextVersionCalculator));
            this.variableProvider = variableProvider ?? throw new ArgumentNullException(nameof(variableProvider));
           // this.gitPreparer = gitPreparer ?? throw new ArgumentNullException(nameof(gitPreparer));

            this.cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
            this.gitVersionCache = gitVersionCache ?? throw new ArgumentNullException(nameof(gitVersionCache));
            this.versionContext = versionContext ?? throw new ArgumentNullException(nameof(versionContext));
        }


        public VersionVariables CalculateVersionVariables(bool? noCache, Config overrideConfig = null)
        {

            //TODO: Need to figure out whether we run Prepare as a seperate command
            //if(prepare)
            //{
            //    gitPreparer.Prepare(); //we need to prepare the repository before using it for version calculation
            //}

            //var gitVersionOptions = options.Value;

            var cacheKey = cacheKeyFactory.Create(overrideConfig);
            var versionVariables = noCache ?? false ? default : gitVersionCache.LoadVersionVariablesFromDiskCache(cacheKey);

            if (versionVariables != null) return versionVariables;

            var semanticVersion = nextVersionCalculator.FindVersion();
            versionVariables = variableProvider.GetVariablesFor(semanticVersion, context.Configuration, context.IsCurrentCommitTagged);

            if (noCache ?? false) return versionVariables;
            try
            {
                gitVersionCache.WriteVariablesToDiskCache(cacheKey, versionVariables);
            }
            catch (AggregateException e)
            {
                log.Warning($"One or more exceptions during cache write:{System.Environment.NewLine}{e}");
            }

            return versionVariables;
        }

    }

}
