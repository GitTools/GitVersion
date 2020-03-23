using System;
using GitVersion.Extensions;
using GitVersion.Extensions.GitVersionInformationResources;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Cache;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionTool : IGitVersionTool
    {
        private readonly ILog log;
        private readonly IGitVersionCache gitVersionCache;
        private readonly INextVersionCalculator nextVersionCalculator;
        private readonly IVariableProvider variableProvider;
        private readonly IGitVersionCacheKeyFactory cacheKeyFactory;
        private readonly IFileSystem fileSystem;
        private readonly IOptions<Arguments> options;
        private readonly GitVersionContext context;
        private readonly IBuildServer buildServer;

        public GitVersionTool(ILog log, IGitVersionCache gitVersionCache, INextVersionCalculator nextVersionCalculator, IVariableProvider variableProvider,
            IGitVersionCacheKeyFactory cacheKeyFactory, IBuildServerResolver buildServerResolver, IFileSystem fileSystem,
            IOptions<Arguments> options, IOptions<GitVersionContext> versionContext)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.gitVersionCache = gitVersionCache ?? throw new ArgumentNullException(nameof(gitVersionCache));
            this.nextVersionCalculator = nextVersionCalculator ?? throw new ArgumentNullException(nameof(nextVersionCalculator));
            this.variableProvider = variableProvider ?? throw new ArgumentNullException(nameof(variableProvider));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            context = versionContext.Value;
            buildServer = buildServerResolver.Resolve();
        }

        public VersionVariables CalculateVersionVariables()
        {
            var arguments = options.Value;

            var cacheKey = cacheKeyFactory.Create(arguments.OverrideConfig);
            var versionVariables = arguments.NoCache ? default : gitVersionCache.LoadVersionVariablesFromDiskCache(cacheKey);

            if (versionVariables != null) return versionVariables;

            versionVariables = ExecuteInternal();

            if (arguments.NoCache) return versionVariables;
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

        public void OutputVariables(VersionVariables variables, Action<string> writter)
        {
            var arguments = options.Value;
            if (arguments.Output.Contains(OutputType.BuildServer))
            {
                buildServer?.WriteIntegration(writter, variables);
            }
            if (arguments.Output.Contains(OutputType.Json))
            {
                switch (arguments.ShowVariable)
                {
                    case null:
                        Console.WriteLine(variables.ToString());
                        break;

                    default:
                        if (!variables.TryGetValue(arguments.ShowVariable, out var part))
                        {
                            throw new WarningException($"'{arguments.ShowVariable}' variable does not exist");
                        }

                        Console.WriteLine(part);
                        break;
                }
            }
        }

        public void UpdateAssemblyInfo(VersionVariables variables)
        {
            var arguments = options.Value;

            if (arguments.UpdateAssemblyInfo)
            {
                using var assemblyInfoUpdater = new AssemblyInfoFileUpdater(arguments.UpdateAssemblyInfoFileName, arguments.TargetPath, variables, fileSystem, log, arguments.EnsureAssemblyInfo);
                assemblyInfoUpdater.Update();
                assemblyInfoUpdater.CommitChanges();
            }
        }

        public void UpdateWixVersionFile(VersionVariables variables)
        {
            var arguments = options.Value;

            if (arguments.UpdateWixVersionFile)
            {
                using var wixVersionFileUpdater = new WixVersionFileUpdater(arguments.TargetPath, variables, fileSystem, log);
                wixVersionFileUpdater.Update();
            }
        }

        public void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo)
        {
            var generator = new GitVersionInformationGenerator(fileSystem);
            generator.Generate(variables, fileWriteInfo);
        }

        private VersionVariables ExecuteInternal()
        {
            var semanticVersion = nextVersionCalculator.FindVersion();
            return variableProvider.GetVariablesFor(semanticVersion, context.Configuration, context.IsCurrentCommitTagged);
        }
    }
}
