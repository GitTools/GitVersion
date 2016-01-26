@echo on

set framework=v4.0.30319
set src=%~dp0src\

"%src%.nuget\nuget.exe" restore %src%

"%SystemDrive%\Windows\Microsoft.NET\Framework\%framework%\MSBuild.exe" "%src%GitVersion.sln"

rmdir /s /q "%tmp%GitVersion"
md "%tmp%GitVersion"

xcopy /E "%~dp0build\NuGetCommandLineBuild\tools" "%tmp%GitVersion"

"%tmp%GitVersion\GitVersion.exe" /l console /output buildserver /updateAssemblyInfo /proj "%src%GitVersion.sln"

rmdir /s /q "%tmp%GitVersion"
