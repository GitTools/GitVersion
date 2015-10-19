# Continuous Deployment
Continuous deployment is the process of checking into master, running all the tests and if everything goes green it is automatically pushed to production.

By default GitVersion is *not* setup to do this. The good news is in v3 of GitVersion this behavior is configurable!

The default behavior for v3 and how v1 & 2 worked was that the version only incremented after a tag, which signified a release. In v3 you can simply switch the default mode in the [configuration](/configuration.md) from `continuous-delivery` to `continuous-deployment` and the version will then increment each commit, giving you the features of GitVersion with continuous deployment.
