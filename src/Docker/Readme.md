# There are 4 variants of docker image :

- based on **microsoft/dotnet-framework:4.7.2-runtime** - Windows Full FX
- based on **microsoft/dotnet:2.1-runtime** - Windows dotnet core
- based on **microsoft/dotnet-framework:4.7.2-runtime** - linux Full FX - on mono
- based on **microsoft/dotnet:2.1-runtime** - linux dotnet core

To run on windows container run this
`docker run --rm -v "$(pwd):c:/repo" gittools/gitversion-fullfx:windows-4.0.0 c:/repo`

`docker run --rm -v "$(pwd):c:/repo" gittools/gitversion-dotnetcore:windows-4.0.0 c:/repo`

To run on linux container run this
`docker run --rm -v "$(pwd):/repo" gittools/gitversion-fullfx:linux-4.0.0 /repo`

`docker run --rm -v "$(pwd):/repo" gittools/gitversion-dotnetcore:linux-4.0.0 /repo`
