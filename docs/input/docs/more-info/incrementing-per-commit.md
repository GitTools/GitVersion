---
Order: 60
Title: Incrementing per commit
---

When using the continuous deployment mode (which will increment the SemVer every
commit) all builds *must* have a pre-release tag, except for builds which are
explicitly tagged as stable.

Then the build metadata (which is the commit count) is promoted to the
pre-release tag. Applying those rules the above commit graph would produce:

```
e137e9		->		1.0.0+0
a5f6c5		->		1.0.1-ci.1
adb29a		->		1.0.1-feature-foo.1 (PR #5 Version: `1.0.1-PullRequest.5+2`)
7c2438		->		1.0.1-feature-foo.2 (PR #5 Version: `1.0.1-PullRequest.5+3`)
5f413b		->		1.0.1-ci.4
d6155b		->		2.0.0-rc.1+4 (Before and after tag)
d53ab6		->		2.0.0-rc.2 (If there was another commit on the release branch it would be 2.0.0-rc.3)
b5d142		->		2.0.0-ci.0 (2.0.0 branch was merged, so master is now at 2.0.0)
```

As you can see the versions now no longer conflict. When you want to create a
stable `2.0.0` release you simply `git tag 2.0.0` then build the tag and it will
produce a stable 2.0.0 package.

For more information/background on why we have come to this conclusion read
[Xavier Decoster's blog post on the subject](http://www.xavierdecoster.com/semantic-versioning-auto-incremented-nuget-package-versions).
