# Octopus deploy
While not a build server, there are a few things to consider when using Octopus Deploy with GitVersion.

 - GitVersion follows [continuous delivery](../../reference/continuous-delivery.md) versioning by default
   - This means builds will keep producing *the same version* with just metadata differing.
     If you try to push every build into Octopus you will have issues

Because Octopus uses NuGet under the covers you cannot publish every build into Octopus deploy. For this we have two possible options:

## Solutions
### 'Release' packages to Octopus deploy
Rather than all builds going into Octopus's NuGet feed, you release builds into it's feed. When you push a package into the NuGet feed you need to tag that release. The next commit will then increment the version.
This has the advantage that if you have a multi-stage deployment pipeline you pick packages which you would like to start through the pipeline, then you can see all the versions which did not make it through the pipeline (for instance, they got to UAT but not production due to a bug being found). In the release notes this can be mentioned or those versions can be skipped.

The steps for this would be

1. Build server has a release build
  - The release build publishes the built NuGet package into a NuGet feed which Octopus deploy consumes
2. The release build should then automatically trigger the first stage of deployments.
  - This could be into a test/uat environment. You can then use Octopus to promote that version through the different environments.
  - You could also do this manually, but if you are triggering a release build you are indicating that is a candidate which you want deployed
3. Tag the source commit with the version. This will cause GitVersion to start building the next version

This approach works well with Semantic Versioning, as you will not be burning version numbers between releases (except if a build fails to get through UAT or something, then the burnt number means something!).

### Configure GitVersion to [increment per commit](../incrementing-per-commit.md)
As mentioned above, this means you will burn multiple versions per release. This might not be an issue for you, but can confuse consumers of your library as the version has semantic meaning.
