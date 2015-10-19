# Octopus Deploy
While not a build server, there are a few things to consider when using Octopus Deploy with GitVersion.

 GitVersion follows [continuous delivery](/reference/continuous-delivery.md) versioning by default. This means builds will keep producing *the same version* with just metadata differing. For example, when you start a new release (say `1.0.0`) with git flow, the branch will start with a semver like `1.0.0-beta.1+0`, and the Octopus NuGet package will have a version of `1.0.0-beta0001`. As you commit changes to this release branch the *metadata* of the semver will increase like so: `1.0.0-beta.1+1`, `1.0.0-beta.1+2`, etc. However, the version of the corresponding Octopus NuGet package will retain the *same* `1.0.0-beta0001` version you started with. The problem is Octopus Deploy will prevent you from deploying these revisions because it sees the same NuGet package version and thinks nothing has changed.

Because Octopus Deploy uses NuGet like this you cannot continue to push revisions in this manner without some intervention (or changes to GitVersion's configuration). To work around this problem we have two possible options:

## Solutions
### Publish your 'release' branch to Octopus deploy
With this approach you will not automatically push every build of your release branch into the NuGet feed that Octopus is consuming. Instead you'll choose which revisions become an Octopus release, and use git tags to keep the NuGet version incrementing as needed. When you have a release version that should be published to Octopus, you push the NuGet package, but also tag the source commit with the desired version. This will cause GitVersion to increase the version for the *next* commit, and you can then combine a series of commits into a new Octopus package.

This has the advantage that if you have a multi-stage deployment pipeline you pick packages which you would like to start through the pipeline, then you can see all the versions which did not make it through the pipeline (for instance, they got to UAT but not production due to a bug being found). In the release notes this can be mentioned or those versions can be skipped.

The following shows an example with the corresponding git commands:

1. Assume your build server has a release build that should be published
  - Current semver is `1.0.0-beta.1+0`
  - Current NuGet package is `1.0.0-beta0001`
2. This NuGet package is pushed to the Octopus deploy feed and starts the deployment lifecycle
3. After deploying, tag the current commit with the semver so GitVersion will start incrementing on the *next* commit
  - `git tag 1.0.0-beta.1`
  - Note, with this tag GitVersion won't increment the reported version until a new commit happens
4. Push this tag to the origin so it's available to the build server
  - `git push --tags` (or just push the specific tag)
3. Now you can commit changes to the release branch and see the version increment (e.g., after 4 commits):
  - Current semver is now `1.0.0-beta.2+4`
  - NuGet version is now `1.0.0-beta0002`
  - Since we tagged the repo in step 3 GitVersion is automatically incrementing both the semver and NuGet versions
4. We can now build this and push the package to Octopus since our NuGet version has incremented
5. Now each time you deploy the package you should re-tag the repo with the version deployed
  - So here we'd tag it like so: `git tag 1.0.0-beta.2`
  - Don't forget to push the tag to the origin: `git push --tags`
6. Repeat as needed

This approach works well with Semantic Versioning, as you will not be burning version numbers between releases (except if a build fails to get through UAT or something, then the burnt number means something!).

### Configure GitVersion to [increment per commit](/more-info/incrementing-per-commit.md)
As mentioned above, this means you will burn multiple versions per release. This might not be an issue for you, but can confuse consumers of your library as the version has semantic meaning.