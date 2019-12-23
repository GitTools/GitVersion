<h1>
    <img src="docs/input/docs/img/package_icon.svg" alt="Tree" height="100">
    GitVersion
</h1>

Versioning when using git, solved. GitVersion looks at your git history and
works out the [semantic version][semver] of the commit being built.

[![Gitter][gitter-badge]][gitter]
[![Build status][appveyor-badge]][appveyor]
[![Build status][azure-pipeline-badge]][azure-pipeline]
[![codecov][codecov-badge]][codecov]
<!-- [![Build status][github-actions-badge]][github-actions] -->

|                                       |                Stable                                              |                                Pre-release                                 |
| ------------------------------------: | :----------------------------------------------------------------: | :------------------------------------------------------------------------: |
|                    **GitHub Release** |                [![GitHub release][gh-rel-badge]][gh-rel]           |                                     -                                      |
|               **GitVersion.Portable** |                     [![Chocolatey][choco-badge]][choco]            |                     [![Chocolatey][choco-pre-badge]][choco]                |
|                    **GitVersionTask** |                            [![NuGet][gvt-badge]][gvt]              |                            [![NuGet][gvt-pre-badge]][gvt]                  |
|            **GitVersion.CommandLine** |                           [![NuGet][gvcl-badge]][gvcl]             |                           [![NuGet][gvcl-pre-badge]][gvcl]                 |
|                   **GitVersion.Tool** |                           [![NuGet][gvgt-badge]][gvgt]             |                           [![NuGet][gvgt-pre-badge]][gvgt]                 |
|                               **Gem** |                              [![Gem][gem-badge]][gem]              |                                     -                                      |
|                          **Homebrew** |                        [![homebrew][brew-badge]][brew]             |                                     -                                      |
|                            **Docker** |               [![Docker Pulls][dockerhub-badge]][dockerhub]        |                                     -                                      |
|               **Azure Pipeline Task** | [![Azure Pipeline Task][az-pipeline-task-badge]][az-pipeline-task] |                                     -                                      |
|                     **Github Action** |             [![Github Action][gh-actions-badge]][gh-actions]       |                                     -                                      |

## Compatibility

GitVersion works on Windows, Linux, and Mac.

Tip: If you get `System.TypeInitializationException: The type initializer for
'LibGit2Sharp.Core.NativeMethods' threw an exception. --->
System.DllNotFoundException: lib/linux/x86_64/libgit2-baa87df.so` in versions prior to 5.0.0

You likely need to install `libcurl3`. Run `sudo apt-get install libcurl3`

## Quick Links

- [Documentation][docs]
- [Contributing][contribute]
- [Why GitVersion][why]
- [Usage][usage]
- [How it works][how]
- [FAQ][faq]
- [Who is using GitVersion][who]

## GitVersion in action!

![README][gv-in-action]

You are seeing:

- Pull requests being built as pre-release builds
- A branch called `release-1.0.0` producing beta v1 packages

## Icon

<a href="https://thenounproject.com/term/tree/13389/" target="_blank">Tree</a>
designed by <a href="http://thenounproject.com/david.chapman" target="_blank">David Chapman</a>
from The Noun Project.

[semver]:                          http://semver.org
[gitter]:                          https://gitter.im/GitTools/GitVersion?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge
[gitter-badge]:                    https://badges.gitter.im/Join+Chat.svg
[appveyor]:                        https://ci.appveyor.com/project/GitTools/gitversion/branch/master
[appveyor-badge]:                  https://ci.appveyor.com/api/projects/status/sxje0wht0cscmn7w/branch/master?svg=true
[azure-pipeline]:                  https://dev.azure.com/GitTools/GitVersion/_build/latest?definitionId=1
[azure-pipeline-badge]:            https://dev.azure.com/GitTools/GitVersion/_apis/build/status/GitTools.GitVersion
[github-actions]:                  https://github.com/GitTools/GitVersion/actions
[github-actions-badge]:            https://github.com/GitTools/GitVersion/workflows/CI/badge.svg
[travis]:                          https://travis-ci.org/GitTools/GitVersion
[travis-badge]:                    https://travis-ci.org/GitTools/GitVersion.svg?branch=master
[codecov]:                         https://codecov.io/gh/GitTools/GitVersion
[codecov-badge]:                   https://codecov.io/gh/GitTools/GitVersion/branch/master/graph/badge.svg
[docs]:                            https://gitversion.net/docs/
[gh-rel]:                          https://github.com/GitTools/GitVersion/releases/latest
[gh-rel-badge]:                    https://img.shields.io/github/release/gittools/gitversion.svg?logo=github
[choco]:                           https://chocolatey.org/packages/GitVersion.Portable
[choco-badge]:                     https://img.shields.io/chocolatey/v/gitversion.portable.svg?logo=nuget
[choco-pre-badge]:                 https://img.shields.io/chocolatey/vpre/gitversion.portable.svg?logo=nuget
[gvt]:                             https://www.nuget.org/packages/GitVersionTask
[gvt-badge]:                       https://img.shields.io/nuget/v/GitVersionTask.svg?logo=nuget
[gvt-pre-badge]:                   https://img.shields.io/nuget/vpre/GitVersionTask.svg?logo=nuget
[gvcl]:                            https://www.nuget.org/packages/GitVersion.CommandLine
[gvcl-badge]:                      https://img.shields.io/nuget/v/GitVersion.CommandLine.svg?logo=nuget
[gvcl-pre-badge]:                  https://img.shields.io/nuget/vpre/GitVersion.CommandLine.svg?logo=nuget
[gvgt]:                            https://www.nuget.org/packages/GitVersion.Tool
[gvgt-badge]:                      https://img.shields.io/nuget/v/GitVersion.Tool.svg?logo=nuget
[gvgt-pre-badge]:                  https://img.shields.io/nuget/vpre/GitVersion.Tool.svg?logo=nuget
[gem-badge]:                       https://img.shields.io/gem/v/gitversion.svg?logo=ruby
[gem]:                             https://rubygems.org/gems/gitversion
[brew]:                            http://brew.sh/
[brew-badge]:                      https://img.shields.io/homebrew/v/gitversion.svg?logo=homebrew
[dockerhub]:                       https://hub.docker.com/r/gittools/gitversion/
[dockerhub-badge]:                 https://img.shields.io/docker/pulls/gittools/gitversion.svg?logo=docker
[az-pipeline-task]:                https://marketplace.visualstudio.com/items?itemName=gittools.gittools
[az-pipeline-task-badge]:          https://img.shields.io/badge/marketplace-gittools.gittools-blue?logo=visual-studio
[gh-actions]:                      https://github.com/marketplace/actions/use-actions
[gh-actions-badge]:                https://img.shields.io/badge/marketplace-use--actions-blue?logo=github
[contribute]:                      https://github.com/GitTools/GitVersion/blob/master/CONTRIBUTING.md
[why]:                             https://gitversion.net/docs/why
[usage]:                           https://gitversion.net/docs/usage/usage
[how]:                             https://gitversion.net/docs/more-info/how-it-works
[faq]:                             https://gitversion.net/docs/faq
[who]:                             https://gitversion.net/docs/who
[gv-in-action]:                    https://raw.github.com/GitTools/GitVersion/master/docs/input/docs/img/README.png
