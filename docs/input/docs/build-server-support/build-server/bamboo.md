---
Order: 30
Title: Bamboo
---

If you use Bamboo then you will have to use GitVersion from the command line, as
there is no actively supported app.  You can use the "Inject Bamboo Variables"
task to read the GitVersion output back into Bamboo. Below are two examples
using the [.NET Core GitVersion global tool](https://www.nuget.org/packages/GitVersion.Tool/).

## Example

### Task: Script

This script can be run on a Linux build host.

**Script body**

```bash
~/.dotnet/tools/dotnet-gitversion > gitversion.txt
sed -i '1d;26d;s/  //;s/"//g;s/,//;s/:/=/' gitversion.txt
```

### Task: Script

**Script body**

This script can be run on a Windows build host using Powershell.

```powershell
(dotnet gitversion | ConvertFrom-Json).PSObject.Properties | ForEach-Object { Write-Output "$($_.Name)=$($_.Value)" } | Out-File -Encoding UTF8 -FilePath gitversion.txt
```

### Task: Inject Bamboo variables Configuration

**Required Properties**

- __Path to properties file__: gitversion.txt
- __Namespace__: GitVersion
- __Scope of the Variables__: Result
