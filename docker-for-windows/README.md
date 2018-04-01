# Windows native docker image

## Why?

A linux docker image is available for GitVersion [here](https://hub.docker.com/r/ilucker/gitversion/)

This is the preferred docker image to use. However, it's also convenient to have a native windows
docker image available, at least until either:

* dotnet core build of GitVersion is available
* linux and native windows containers can be run side-by-side (see [--platform flag](https://blog.docker.com/2018/02/docker-for-windows-18-02-with-windows-10-fall-creators-update/#h.kiztgok2bqti))

## Use docker image

``` powershell
docker run --rm -v "$(pwd):C:\repo" gittools/gitversion-win
```

The above command is the equivalent to running (assuming you have GitVersion.exe in your `$PATH`):

``` cmd
GitVersion
```

## Build docker image

1. Open powershell prompt
2. Change directory to the `docker-for-windows` directory
    * eg: `cd C:\Git\GitVersion\docker-for-windows`
3. Run build.ps1 with the appropriate arguments
    * eg: `.\build.ps1 -Version 4.0.0-beta.13 -VersionZip 4.0.0-beta0013`

## Publish image

1. Open powershell prompt
2. Login to docker registry you will be pushing to (see [docker login](https://docs.docker.com/engine/reference/commandline/login/) command)
3. Change directory to the `docker-for-windows` directory
    * eg: `cd C:\Git\GitVersion\docker-for-windows`
3. Run build.ps1 with the appropriate arguments
    * eg: `.\build.ps1 -Version 4.0.0-beta.13 -VersionZip 4.0.0-beta0013` -Publish