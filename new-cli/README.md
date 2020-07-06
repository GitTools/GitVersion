Ways the commands can be run from command line

```bash
dotnet run --project .\GitVersion.Cli\ --  --help

dotnet run --project .\GitVersion.Cli\ --  output --help

dotnet run --project .\GitVersion.Cli\ --  output assemblyinfo --help

dotnet run --project .\GitVersion.Cli\ --  output assemblyinfo --assemblyinfo-file GlobalAssemblyInfo.cs --output-dir c:\output

"1.2.3-beta4" | dotnet run --project .\GitVersion.Cli\ --  output assemblyinfo --assemblyinfo-file GlobalAssemblyInfo.cs --output-dir c:\output
```
