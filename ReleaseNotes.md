# v1.2.1
Fixing an issue with lib2gitsharp - [#246](https://github.com/Particular/GitVersion/issues/246)

# v1.2.0
Support updating a single common AssemblyInfo.cs [#227](https://github.com/Particular/GitVersion/pull/227) thanks @hmemcpy
Note we do not have the assembly info switches in the command line help at the moment, see [#237](https://github.com/Particular/GitVersion/issues/237)

Usage for updating assembly info is /updateAssemblyInfo if you want to specify the file it is 

```
/updateassemblyinfo ..\src\CommonAssemblyInfo.cs or /updateassemblyinfo "C:\src\CommonAssemblyInfo.cs"
```

# v1.1.0
Bug fix with release branches in GitHubFlow

# v1.1.0 

 - [#222](https://github.com/Particular/GitVersion/pull/222) - Log is printed to console on error
 - [#211](https://github.com/Particular/GitVersion/pull/211) - should get exact version from tag contributed by Simon Cropp ([SimonCropp](https://github.com/SimonCropp))
 - [#210](https://github.com/Particular/GitVersion/pull/210) - Added additional variables for NuGet (see #201) contributed by Geert van Horrik ([GeertvanHorrik](https://github.com/GeertvanHorrik))
 - [#208](https://github.com/Particular/GitVersion/pull/208) - Added support for NextVersion.txt in GitFlow and dynamic repositories contributed by Geert van Horrik ([GeertvanHorrik](https://github.com/GeertvanHorrik))
 - [#205](https://github.com/Particular/GitVersion/issues/205) - Auto-generate patch version with every commit to master.
 - [#204](https://github.com/Particular/GitVersion/pull/204) - Support stash contributed by Gary Ewan Park ([gep13](https://github.com/gep13))
 - [#200](https://github.com/Particular/GitVersion/pull/200) - Added skipIfNotDefined for upcoming Continua CI v1.5 contributed by Geert van Horrik ([GeertvanHorrik](https://github.com/GeertvanHorrik))
 - [#195](https://github.com/Particular/GitVersion/pull/195) - Adding support for Syntevo SmartGit/Hg's GitFlow merge commit message co... contributed by ([Martaver](https://github.com/Martaver))
 - [#191](https://github.com/Particular/GitVersion/pull/191) - Improved the Ruby Gem to make it usable from a Rakefile contributed by Alexander Groß ([agross](https://github.com/agross))
 - [#189](https://github.com/Particular/GitVersion/pull/189) - Better feature branch support
 - [#186](https://github.com/Particular/GitVersion/issues/186) - No way to have an x.x.0 release using GitHubFlow
 - [#185](https://github.com/Particular/GitVersion/pull/185) - Fixed bug in SemanticVersion.Compare contributed by ([gius](https://github.com/gius))
 - [#184](https://github.com/Particular/GitVersion/pull/184) - Add support for nuget special version limitations contributed by Anthony Ledesma ([arledesma](https://github.com/arledesma))
 - [#183](https://github.com/Particular/GitVersion/pull/183) - updated spec version contributed by ([danielmarbach](https://github.com/danielmarbach))
 - [#87](https://github.com/Particular/GitVersion/issues/87) - only perform PerformPreProcessingSteps once per solution instance

Commits: [493a204e81...232ef26ca3](https://github.com/Particular/GitVersion/compare/493a204e81...232ef26ca3)

# v1.0.0
Initial Release
