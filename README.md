# ![GitVersion – From git log to SemVer in no time][banner]

Versioning when using Git, solved. GitVersion looks at your git history and
works out the [Semantic Version][semver] of the commit being built.

[![Build status][azure-pipeline-badge]][azure-pipeline]
[![Build status][github-actions-badge]][github-actions]
[![codecov][codecov-badge]][codecov]

| Artifact                   | Stable                                                             |
|:---------------------------|:-------------------------------------------------------------------|
| **GitHub Release**         | [![GitHub release][gh-rel-badge]][gh-rel]                          |
| **GitVersion.Portable**    | [![Chocolatey][choco-badge]][choco]                                |
| **GitVersion.Tool**        | [![NuGet][gvgt-badge]][gvgt]                                       |
| **GitVersion.MsBuild**     | [![NuGet][gvt-badge]][gvt]                                         |
| **Homebrew**               | [![homebrew][brew-badge]][brew]                                    |
| **Winget**                 | [![winget][winget-badge]][winget]                                  |
| **Azure Pipeline Task**    | [![Azure Pipeline Task][az-pipeline-task-badge]][az-pipeline-task] |
| **Github Action**          | [![Github Action][gh-actions-badge]][gh-actions]                   |
| **Docker**                 | [![Docker Pulls][dockerhub-badge]][dockerhub]                      |

## Compatibility

GitVersion works on Windows, Linux, and Mac.

## Quick Links

* [Documentation][docs]
* [Contributing][contribute]
* [Why GitVersion][why]
* [Usage][usage]
* [How it works][how]
* [FAQ][faq]
* [Who is using GitVersion][who]

## GitVersion in action!

![README][gv-in-action]

You are seeing:

* Pull requests being built as pre-release builds
* A branch called `release-1.0.0` producing beta v1 packages

## Icon

[Tree][app-icon]
designed by [David Chapman][app-icon-author]
from The Noun Project.

[semver]: https://semver.org

[azure-pipeline]: https://dev.azure.com/GitTools/GitVersion/_build/latest?definitionId=1

[azure-pipeline-badge]: https://dev.azure.com/GitTools/GitVersion/_apis/build/status/GitTools.GitVersion

[github-actions]: https://github.com/GitTools/GitVersion/actions

[github-actions-badge]: https://github.com/GitTools/GitVersion/workflows/CI/badge.svg

[codecov]: https://codecov.io/gh/GitTools/GitVersion

[codecov-badge]: https://codecov.io/gh/GitTools/GitVersion/branch/main/graph/badge.svg

[docs]: https://gitversion.net/docs/

[gh-rel]: https://github.com/GitTools/GitVersion/releases/latest

[gh-rel-badge]: https://img.shields.io/github/release/gittools/gitversion.svg?logo=github

[choco]: https://chocolatey.org/packages/GitVersion.Portable

[choco-badge]: https://img.shields.io/chocolatey/v/gitversion.portable.svg?logo=nuget

[gvt]: https://www.nuget.org/packages/GitVersion.MsBuild

[gvt-badge]: https://img.shields.io/nuget/v/GitVersion.MsBuild.svg?logo=nuget

[gvgt]: https://www.nuget.org/packages/GitVersion.Tool

[gvgt-badge]: https://img.shields.io/nuget/v/GitVersion.Tool.svg?logo=nuget

[brew]: https://formulae.brew.sh/formula/gitversion

[brew-badge]: https://img.shields.io/homebrew/v/gitversion.svg?logo=homebrew

[winget]: https://github.com/microsoft/winget-pkgs/tree/master/manifests/g/GitTools/GitVersion

[winget-badge]: https://img.shields.io/winget/v/GitTools.GitVersion.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAMeSURBVHgBrVS9axRREJ/3ds+QRPLtBxolYkRJYSNiKZGoaGdtI+gfIGhCCEgupb2llonYCmJhYZMDaysxClHwQriYXKI5xezOOPM+dt9uLlWyd4/3MW9+M/Obmacg+F486JolgCrs41Osf/95a87vdSjcL3g7jDjc1Dew6Itcl5VSQGTVyUn2/tTu3dvqhZGK1u95OQIH8y3vII7fqn5aNhTFSt87QHD5RhympWi58RciZUMTOtDyYffhgkprL6QAms9I9MmmV319MzXL+yoQWoHw7A34M/4jYS5zcpIzXqM7K+jLUFhVS68fkT+UIU6hUyyCOWVEk2raA9SUAeV4MWFqN2A9tYKit15OJc/DSMMoFOQ6MRoD9lIYib+IzkPd0Q/nrk8ZJ768ewpJq5Hd81FmkWcGuNGMAUxA5nCIl1aGkCYpdJ+6ApWuQah0D5p1mhbvp4xBfu9lacIR8AYyejDj0EZu98hzkqQZJQIuQykq5IqCHDgubQ5yvotGfF7CqOSaOOX1oFwUYa7Ysdh6iUEV5OWZe4aOMnsmgBK+Unl1hZUTFoJGspyR80pm2QvvfaMTcGZimhM8wGdY8Ex39sPpqw/h6MU7Lh9o9U20eU5jTBIXFphIvCcdQ+dh+PJdA3j2xgw0vn3Moog6B2D05hPoOHwEeoYvwernGqS/vu/qG7lv+6BQ89bQen0Jmmt16Bk4Doe4ck6OjbOn9rU9MXYtA9laX4HW9hZUJOmlfMhPZwnMSi6xycFt+PBqBrZ+1gu9IVT40Vz7AYvz3But1UyfEPOC4Fnnh5gdSl9odqc3akLt5TQbWXHy3IiA1xi8L9qASIM5SznxkvywF7RvCD/IC3kdaWKAJiwuTBaMbHJUtflJdmAdYk0uqUkQhWOCcTSVKErDbpYqYIB+iWThMfzeWIFNQ8ukMVyJwEWMAcVFPLX47Db5ms3q15WieYpds/3bSaGx+cc8WUO9FQZXbR/E8isck9AC1LbE8obhixHBsb6Ka16bh7KBwnPtZp0gzvnWNxSlSd4oJi85nxBQkVHjGxRts/nkksFK5/4DGLPsj2fzroIAAAAASUVORK5CYII=

[dockerhub]: https://hub.docker.com/r/gittools/gitversion/

[dockerhub-badge]: https://img.shields.io/docker/pulls/gittools/gitversion.svg?logo=docker

[az-pipeline-task]: https://marketplace.visualstudio.com/items?itemName=gittools.gittools

[az-pipeline-task-badge]: https://img.shields.io/badge/marketplace-gittools-blue?logo=data:application/pdf;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAGXRFWHRTb2Z0d2FyZQB3d3cuaW5rc2NhcGUub3Jnm+48GgAABJlJREFUWIXFVk1sVFUU/u6dNzOd6fx2ftoGFNIfNa1prCW4ALHsGolIYmjQGH/CwmUXBl2gK0OihoUYE5G40GAEQ7QJ0IobQSEkLYLFH4QabSpUptPpTDs/773pzLvHRTvDm3lvpjOtxpOcTN69557vu+eec+YwIsL/KdK/4aRzz8WupubA4xKDrRZ7LS8yYxPZ4/RDX27dBLa8PLGnu7fjKxBYPee2ijgH8PG6CGwZvOptfbD1pNDqAwcAboUbWOcTeDb4R8CYfS1ZJLTl3zUT2Lr/xkvhVt82Ekb4TSGG9o12OOwc0cQSfp3OQVZLgySwDgLtg1e9nR0tR02w0bPZgofbXcVvt9OKjWGBr8cXIavcwIBjDbKhxTcCMLsgQK92K6GrzQUilKhN4ujeZCuxNUSg8/kxj1coq5aRsAdeCLUEtpm1D4+TVczGkM8Govy9BQYqEuh78WKPx2a9TmioCs4ZRyDYhErNS1YJlfqarGol58RKCCQAWNJYu8Sq5zJjDI3uAAioCJKQgWhCRchvvMjkXypKcmalCviycwuVv2e5Wi0OcHtDVRshGL77SUE0oRbfXxOEa5MpTEVLbQsiAUBeA6pFwMIscPgCFW+ul7TKMHpFQdClQJIYFtMEJWfMDEMSmpXUcnQApzsIgJkSaHJzLGQ0CKEDIYa5ZNGDqd98Lq/qCOQhhHlFNthcsNjsECYMgz4JPncj/F6BqZk08lrNPZEW4okLRQJCWAjceNjCOBzeJtPoNPuscLud0AhgxOF2cswntdWRibKxO9F9M9/03ywSAPEY9DUKAIzB5Q4CbKVgdRLySnC5HCi04WRaQawGcDWTvRyfS+2aGu5bKMIUavOBfZeGALxX2LBZHPAEmw1OQn4LvK5GEJZfN5mWEU3kDXZ6EUS5ZDQxdONkz4fle0zfHNr2fv8qZ+wwANgtjXCHwgZwXxn47CrgWVkej0eiA9NntyfM9ll5V2vbe+ktgN5gYLccDf5Jp8/zFACE/ZLh5rOJXEVgIkJ6fuHAbyd6DlcjaEj9P09tfxOE19Ss49FfjnfvlhfTX4R8ZeApGZF4zvCnU9AlRUE8cgdJFv+kanjMIlAuTx+c6gmGAtcz2XvglW5ORMgkYlCzGRAj5HMiNHO6P1bNf9V5YODgdGtvR/AKGEMkQZiNpRGp8OZLiozUQgyi0ORrbAkcAD44m3royHDq7SNfJh/Tb547tOnu3zH5MxKUi8bme/+Yip8QgqBXTRBSsSgW4rPIUx6CqKg1EyANnYzjdWblveUGnx5o3v/zzdv3jxzaPHHr8+7nMsnkGVq54JKiYP7ubWSyaQiQIRdqkZpGspF3uyIAwAZP2VqV8H25jAJihCWRqTqNS5JzVRp1zYRhJbxLI3pE0TIrK3VP4wapayacPfPEsBD0iv6dq2kyubrP0ggQnn3/dLp0KSd+HHrGM1b4nhvdeSzw5Hkw4KN6yNdGALQDhB0lS5y9A2BMvzQ/uvNY08AFDpCht+ulQZOzNREQsjLOnY7dZgbcit/N1uPn+o96B769BkLAbJ/Ap+Ln+9Nme3pZtRP+1/IPO814AQ1WwqwAAAAASUVORK5CYII=

[gh-actions]: https://github.com/marketplace/actions/gittools

[gh-actions-badge]: https://img.shields.io/badge/marketplace-gittools-blue?logo=github

[contribute]: https://github.com/GitTools/GitVersion/blob/main/CONTRIBUTING.md

[why]: https://gitversion.net/docs/learn/why

[usage]: https://gitversion.net/docs/usage

[how]: https://gitversion.net/docs/learn/how-it-works

[faq]: https://gitversion.net/docs/learn/faq

[who]: https://gitversion.net/docs/learn/who

[gv-in-action]: https://raw.githubusercontent.com/GitTools/GitVersion/master/docs/input/docs/img/README.png

[banner]: https://raw.githubusercontent.com/GitTools/graphics/master/GitVersion/banner-1280x640.png

[app-icon]: https://thenounproject.com/term/tree/13389/

[app-icon-author]: https://thenounproject.com/david.chapman
