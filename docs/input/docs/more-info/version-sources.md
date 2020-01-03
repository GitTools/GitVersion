---
Order: 50
Title: Version Sources
---

GitVersion has a two step process for calculating the version number. First it
calculates the base version, which is then used to calculate what the next
version should be.

The logic of GitVersion is something like this:

- Is the current commit tagged
  - Yes: Use the tag as the version
  - No: continue
- Calculate the base version (highest version from all the sources)
- Increment version if needed based on branch config
- Calculate the build metadata (everything after the +) and append to the
calculated version

## Version Sources

### Tag name

Returns the version numbers extracted from the current branch's tags.

Will increment: true

### Version in branch name

Returns the version number from the branch's name.

Will increment: false

### Merge message

Returns the version number of any branch (with a version number in its name)
merged into the current branch.

Will increment: depends on the value of `prevent-increment-of-merged-branch-version`

### GitVersion.yml

Returns the value of the `next-version` property in the config file.

Will increment: false

### Develop branch

For the develop branch, i.e. marked with `is-develop: true`

- Returns the version number extracted from any child release-branches, i.e.
those marked with `is-release-branch: true`
- Returns the version number of any tags on the master branch

Will increment: true

### Fallback

Returns the version number `0.1.0`.

Will increment: false

### Others?

Want more ways to increment the version? Open an issue with your idea and submit
a pull request!
