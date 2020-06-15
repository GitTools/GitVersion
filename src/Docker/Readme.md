# There are 4 variants of docker image

- based on **microsoft/dotnet-framework:4.7.2-runtime** - Windows Full FX
- based on **microsoft/dotnet:2.1-runtime** - Windows dotnet core
- based on **mono:5.18** - Linux Full FX - on mono
- based on **microsoft/dotnet:2.1-runtime** - Linux dotnet core
- based on **centos:7** - linux dotnet core

To run on windows container run this
`docker run --rm -v "$(pwd):c:/repo" gittools/gitversion:latest-windows-net472 c:/repo`

`docker run --rm -v "$(pwd):c:/repo" gittools/gitversion:latest-windows-netcoreapp2.1 c:/repo`

To run on linux container run this
`docker run --rm -v "$(pwd):/repo" gittools/gitversion:latest-linux-net472 /repo`

`docker run --rm -v "$(pwd):/repo" gittools/gitversion:latest-linux-netcoreapp2.1 /repo`
