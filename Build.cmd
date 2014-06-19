@echo on

set framework=v4.0.30319

"%~dp0.nuget\nuget.exe" restore

"%SystemDrive%\Windows\Microsoft.NET\Framework\%framework%\MSBuild.exe" "%~dp0GitVersion.sln"

mkdir "%~dp0GitVersionExe\bin\Intermediate"
cp "%~dp0GitVersionExe\bin\Debug\GitVersion.exe" "%~dp0GitVersionExe\bin\Intermediate\GitVersion.exe"

"%~dp0GitVersionExe\bin\Intermediate\GitVersion.exe" /l console /output buildserver /updateAssemblyInfo /proj "%~dp0GitVersion.sln"
