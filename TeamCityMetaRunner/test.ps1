$scriptpath = Split-Path $MyInvocation.MyCommand.Path
.\GitVersion.ps1 -workingDir "$scriptpath\..\" -output json -logFile "$scriptpath\log.txt" -outputFile "$scriptpath\json.txt"
.\GitVersion.ps1 -workingDir "$scriptpath\..\" -output buildserver -exec "c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" -execArgs "$scriptpath\..\GitVersion.sln /t:Rebuild /p:Configuration=Release"
.\GitVersion.ps1 -workingDir "$scriptpath\..\" -output buildserver -proj "$scriptpath\..\GitVersion.sln" -projArgs "/t:Rebuild /p:Configuration=Release" -updateAssemblyInfo "true"
