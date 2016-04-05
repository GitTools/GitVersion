# Version Sources
GitVersion has a two step process for calculating the version number, the first is to calculate the base version which is used to then calculate what the next version should be.

The logic of GitVersion is something like this:

 - Is the current commit tagged
   - Yes: Use the tag as the version
   - No: continue
 - Calculate the base version (highest version from all the sources)
 - Increment version if needed based on branch config
 - Calculate the build metadata (everything after the +) and append to the calcuated version

## Version Sources
### Highest Accessible Tag
GitVersion will find all tags on the current branch and return the highest one.

Will increment: true

### Version in branch name
If the branch has a version in it, then that version will be returned.

Will increment: false

### Merge message
If a branch with a version number in it is merged into the current branch, that version number will be used.

Will increment: false

### GitVersion.yml
If the `next-version` property is specified in the config file, it will be used as a version source.

Will increment: false

### Others?
Want more ways to increment the version? Open an issue with your idea and submit a pull request!
