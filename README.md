![GitVersion – From git log to SemVer in no time][banner]

Versioning when using Git, solved. GitVersion looks at your git history and
works out the [Semantic Version][semver] of the commit being built.

[![Build status][azure-pipeline-badge]][azure-pipeline]
[![Build status][github-actions-badge]][github-actions]
[![codecov][codecov-badge]][codecov]
[![Gitter][gitter-badge]][gitter]

| Artifact                   |                Stable                                              |   |
| :------------------------- | :----------------------------------------------------------------- | - |
| **GitHub Release**         |                [![GitHub release][gh-rel-badge]][gh-rel]           |
| **GitVersion.Portable**    |                     [![Chocolatey][choco-badge]][choco]            |
| **GitVersion.Tool**        |                           [![NuGet][gvgt-badge]][gvgt]             |
| **GitVersion.CommandLine** |                           [![NuGet][gvcl-badge]][gvcl]             |
| **GitVersion.MsBuild**     |                            [![NuGet][gvt-badge]][gvt]              | Known as [GitVersionTask][gitversiontask] before v5.6.0 |
| **Homebrew**               |                        [![homebrew][brew-badge]][brew]             |
| **Azure Pipeline Task**    | [![Azure Pipeline Task][az-pipeline-task-badge]][az-pipeline-task] |
| **Github Action**          |             [![Github Action][gh-actions-badge]][gh-actions]       |
| **Docker**                 |               [![Docker Pulls][dockerhub-badge]][dockerhub]        |

## Compatibility

GitVersion works on Windows, Linux, and Mac.

## Quick Links

*   [Documentation][docs]
*   [Contributing][contribute]
*   [Why GitVersion][why]
*   [Usage][usage]
*   [How it works][how]
*   [FAQ][faq]
*   [Who is using GitVersion][who]

## GitVersion in action!

![README][gv-in-action]

You are seeing:

*   Pull requests being built as pre-release builds
*   A branch called `release-1.0.0` producing beta v1 packages

## Icon

[Tree][app-icon]
designed by [David Chapman][app-icon-author]
from The Noun Project.

[semver]:                          http://semver.org
[gitter]:                          https://gitter.im/GitTools/GitVersion?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge
[gitter-badge]:                    https://badges.gitter.im/Join+Chat.svg
[azure-pipeline]:                  https://dev.azure.com/GitTools/GitVersion/_build/latest?definitionId=1
[azure-pipeline-badge]:            https://dev.azure.com/GitTools/GitVersion/_apis/build/status/GitTools.GitVersion
[github-actions]:                  https://github.com/GitTools/GitVersion/actions
[github-actions-badge]:            https://github.com/GitTools/GitVersion/workflows/Build/badge.svg
[codecov]:                         https://codecov.io/gh/GitTools/GitVersion
[codecov-badge]:                   https://codecov.io/gh/GitTools/GitVersion/branch/main/graph/badge.svg
[docs]:                            https://gitversion.net/docs/
[gh-rel]:                          https://github.com/GitTools/GitVersion/releases/latest
[gh-rel-badge]:                    https://img.shields.io/github/release/gittools/gitversion.svg?logo=github
[choco]:                           https://chocolatey.org/packages/GitVersion.Portable
[choco-badge]:                     https://img.shields.io/chocolatey/v/gitversion.portable.svg?logo=nuget
[gvt]:                             https://www.nuget.org/packages/GitVersion.MsBuild
[gvt-badge]:                       https://img.shields.io/nuget/v/GitVersion.MsBuild.svg?logo=nuget
[gitversiontask]:                  https://www.nuget.org/packages/GitVersionTask/
[gvcl]:                            https://www.nuget.org/packages/GitVersion.CommandLine
[gvcl-badge]:                      https://img.shields.io/nuget/v/GitVersion.CommandLine.svg?logo=nuget
[gvgt]:                            https://www.nuget.org/packages/GitVersion.Tool
[gvgt-badge]:                      https://img.shields.io/nuget/v/GitVersion.Tool.svg?logo=nuget
[brew]:                            https://formulae.brew.sh/formula/gitversion
[brew-badge]:                      https://img.shields.io/homebrew/v/gitversion.svg?logo=homebrew
[dockerhub]:                       https://hub.docker.com/r/gittools/gitversion/
[dockerhub-badge]:                 https://img.shields.io/docker/pulls/gittools/gitversion.svg?logo=docker
[az-pipeline-task]:                https://marketplace.visualstudio.com/items?itemName=gittools.gittools
[az-pipeline-task-badge]:          https://img.shields.io/badge/marketplace-gittools.gittools-blue?logo=azure-pipelines
[gh-actions]:                      https://github.com/marketplace/actions/gittools
[gh-actions-badge]:                https://img.shields.io/badge/marketplace-gittools-blue?logo=github
[contribute]:                      https://github.com/GitTools/GitVersion/blob/main/CONTRIBUTING.md
[why]:                             https://gitversion.net/docs/learn/why
[usage]:                           https://gitversion.net/docs/usage
[how]:                             https://gitversion.net/docs/learn/how-it-works
[faq]:                             https://gitversion.net/docs/learn/faq
[who]:                             https://gitversion.net/docs/learn/who
[gv-in-action]:                    https://raw.githubusercontent.com/GitTools/GitVersion/master/docs/input/docs/img/README.png
[banner]:                          https://raw.githubusercontent.com/GitTools/graphics/master/GitVersion/banner-1280x640.png
[app-icon]:                        https://thenounproject.com/term/tree/13389/
[app-icon-author]:                 http://thenounproject.com/david.chapman
