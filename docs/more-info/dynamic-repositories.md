# Dynamic repositories
GitVersion.exe requires access to the Git repository in order to do / infer all the lovely things that it does.

Well maybe that's not so much of a revelation..

_But did you know_ that in some circumstances, you may find that when your code is built, the build process isn't aware of the Git repository at all - i.e _there is no Git Repository available locally to the build!_

For an example of one such circumstance, you can have a read about [Team City's checkout mode: Automatically on Server](https://confluence.jetbrains.com/display/TCD7/VCS+Checkout+Mode)

So `how is GitVersion meant to work in that scenario?` - well it needs to be able to obtain a copy of the Git repo on the fly and not rely on the build system making one locally available..

Enter: **Dynamically Obtained Repo's**

## Tell GitVersion.exe how to obtain your repository

Unless you tell GitVersion.exe how to obtain the Git repository, it will assume that the Git repository is already available locally to the process - i.e. it will assume there is already a ".git" folder present, and it will use it.

To tell GitVersion.exe to obtain the repository on the fly, you need to call `GitVersion.exe` with the following arguments:

* /url [the url of your git repo]
* /u [authentication username]
* /p [authentication password]
* /b [branch name]
* /c [commit id]

Please note that these arguments are described when calling `GitVersion.exe /?`.

Also, be aware that if you don't specify the `/b` argument (branch name) then GitVersion will currently fallback to targeting whatever the default branch name happens to be for the repo. This could lead to incorrect results, so for that reason it's recommended to always explicitly specify the branch name.

NB: In Team City, the VCS branch is available from a configuration parameter with the following name:

`teamcity.build.vcs.branch.<VCS root ID>`

Where `<VCS root ID>` is the `VCS root ID` as described on the Configuring `VCS Roots` page.

## Where is the repository stored
GitVersion will checkout the dynamic repository into the %tmp% directory and just keep that dynamic repository up to date, this saves recloning the repository every build.
