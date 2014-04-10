@echo on

set framework=v4.0.30319

"%~dp0.nuget\nuget.exe" restore

"%SystemDrive%\Windows\Microsoft.NET\Framework\%framework%\MSBuild.exe" "%~dp0GitVersion.sln"

mkdir "%~dp0GitVersion\bin\Intermediate"
cp "%~dp0GitVersion\bin\Debug\GitVersion.exe" "%~dp0GitVersion\bin\Intermediate\GitVersion.exe"

"%~dp0GitVersion\bin\Intermediate\GitVersion.exe" /l console /output buildserver /updateAssemblyInfo /proj "%~dp0GitVersion.sln"
