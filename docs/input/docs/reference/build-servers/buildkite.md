---
Order: 40
Title: Buildkite
Description: Details on the Buildkite support in GitVersion
RedirectFrom: docs/build-server-support/build-server/buildkite
---

If you use [Buildkite][buildkite] then you will have to use GitVersion from the command line as there is currently no GitVersion Buildkite plugin.

## Gotchas

By default Buildkite calls `git fetch` with the flags `-v --prune` which can cause issues on new build agents since branches or tags might not be available locally on the build agent when GitVersion runs. This can be fixed by altering the [Buildkite agent configuration][configuration] either by:
- Setting the environment variable `BUILDKITE_GIT_FETCH_FLAGS` to `-v --tags`
- Setting configuration value `git-fetch-flags` to `-v --tags` in your agent configuration file

If you are running GitVersion in a docker container make sure to propogate the `BUILDKITE` and `BUILDKITE_BRANCH` environment variables (c.f. example below).

## Example

There are many ways to run GitVersion in a Buildkite pipeline. One way using the GitVersion docker image and using [build meta-data][meta-data] to share version info between build steps. Such a pipeline might look like the following:

```yaml
env:
  BUILDKITE_GIT_FETCH_FLAGS: "-v --tags"

steps:
  - label: "Calculate version"
    command: buildkite-agent meta-data set "GitVersion_SemVer" $(./dotnet-gitversion -showvariable SemVer)
    plugins:
      - docker#v3.9.0:
        image: "gittools/gitversion"
        environment:
          - "BUILDKITE"
          - "BUILDKITE_BRANCH"

  - wait

  - label: "Use calculated version"
    command: echo "Calculated version is $(buildkite-agent meta-data get "GitVersion_SemVer")"
```

Another way could be via the [Buildkite hooks][hooks]. Adding the following line to the `.buildkite/hooks/post-checkout` file:

```
eval $(gitversion | jq -r 'to_entries[] | "buildkite-agent meta-data set GitVersion_\(.key) \(.value)"')
```

Assuming your Buildkite agent has dotnet and gitversion installed and on the path, all the calculated GitVersion variables will have a corresponding meta-data key set.


[buildkite]: https://buildkite.com/
[configuration]: https://buildkite.com/docs/agent/v3/hooks
[hooks]: https://buildkite.com/docs/agent/v3/hooks
[meta-data]: https://buildkite.com/docs/agent/v3/cli-meta-data
