using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Git;
using Cake.Kudu;
using Cake.Kudu.KuduSync;
using Common.Utilities;

namespace Docs.Tasks
{
    [TaskName(nameof(PublishDocs))]
    [TaskDescription("Published the docs changes to docs specific branch")]
    [IsDependentOn(typeof(BuildDocs))]
    public sealed class PublishDocs : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.DirectoryExists(Paths.Docs), "Wyam documentation directory is missing");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            PublishDocumentation(context);
        }

        private static void PublishDocumentation(BuildContext context)
        {
            const string publishBranchName = "gh-pages";
            var sourceCommit = context.GitLogTip("./");

            var publishFolder = context.MakeAbsolute(Paths.ArtifactsDocs.Combine("_published").Combine(DateTime.Now.ToString("yyyyMMdd_HHmmss")));
            context.Information("Publishing Folder: {0}", publishFolder);
            context.Information("Getting publish branch...");
            context.GitClone("https://github.com/gittools/GitVersion", publishFolder, new GitCloneSettings
            {
                BranchName = publishBranchName
            });

            context.Information("Sync output files...");
            context.Kudu().Sync(context.MakeAbsolute(Paths.ArtifactsDocs.Combine("preview")), publishFolder, new KuduSyncSettings
            {
                ArgumentCustomization = args => args.Append("--ignore").AppendQuoted(".git;CNAME;_git2")
            });

            if (context.GitHasUncommitedChanges(publishFolder))
            {
                context.Information("Stage all changes...");
                context.GitAddAll(publishFolder);

                if (context.GitHasStagedChanges(publishFolder))
                {
                    context.Information("Commit all changes...");
                    context.GitCommit(
                        publishFolder,
                        sourceCommit.Committer.Name,
                        sourceCommit.Committer.Email,
                        $"Continuous Integration Publish: {sourceCommit.Sha}\r\n{sourceCommit.Message}"
                    );

                    PublishToGitHub(context, publishFolder, publishBranchName);
                }
            }
        }
        
        private static void PublishToGitHub(BuildContext context, DirectoryPath publishFolder, string publishBranchName)
        {
            var username = context.Credentials?.GitHub?.UserName;
            if (string.IsNullOrEmpty(username))
            {
                throw new InvalidOperationException("Could not resolve Github username.");
            }

            var token = context.Credentials?.GitHub?.Token;
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Could not resolve Github token.");
            }

            context.Information("Pushing all changes...");
            context.GitPush(publishFolder, username, token, publishBranchName);
        }
    }
}
