using Cake.DotNetLocalTools.Module;

namespace Common.Utilities;

public static class ServicesExtensions
{
    public static CakeHost UseRootDirectory(this CakeHost host)
    {
        host = host.ConfigureServices(services => services.UseWorkingDirectory(Extensions.GetRootDirectory()));
        return host;
    }

    public static CakeHost InstallToolsFromRootManifest(this CakeHost host)
    {
        host = host.UseModule<LocalToolsModule>().InstallToolsFromManifest(Extensions.GetRootDirectory().CombineWithFilePath("dotnet-tools.json").FullPath);
        return host;
    }

    public static CakeHost InstallNugetTool(this CakeHost host, string toolName, string toolVersion)
    {
        var toolUrl = new Uri($"nuget:?package={toolName}&version={toolVersion}");
        return host.ConfigureServices(services => services.UseTool(toolUrl));
    }
}
