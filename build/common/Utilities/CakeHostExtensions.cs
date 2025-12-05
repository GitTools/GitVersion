using Cake.DotNetLocalTools.Module;

namespace Common.Utilities;

public static class ServicesExtensions
{
    extension(CakeHost host)
    {
        public CakeHost UseRootDirectory()
        {
            host = host.ConfigureServices(services => services.UseWorkingDirectory(Extensions.GetRootDirectory()));
            return host;
        }

        public CakeHost InstallToolsFromRootManifest()
        {
            host = host.UseModule<LocalToolsModule>().InstallToolsFromManifest(Extensions.GetRootDirectory().CombineWithFilePath(".config/dotnet-tools.json").FullPath);
            return host;
        }

        public CakeHost InstallNugetTool(string toolName, string toolVersion)
        {
            var toolUrl = new Uri($"nuget:?package={toolName}&version={toolVersion}");
            return host.ConfigureServices(services => services.UseTool(toolUrl));
        }
    }
}
