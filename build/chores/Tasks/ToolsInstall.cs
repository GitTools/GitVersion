using Cake.Json;
using Newtonsoft.Json.Linq;

namespace Chores.Tasks;

[TaskName(nameof(ToolsInstall))]
[TaskDescription("Install dotnet global tools to 'tools' folder")]
public class ToolsInstall : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var json = context.ParseJsonFromFile("dotnet-tools.json");
        var jToken = json["tools"];
        if (jToken == null) return;

        var tools = jToken.Select(x => (JProperty)x).ToDictionary(x => x.Name, x => x.Value["version"]?.Value<string>());
        foreach (var (toolName, version) in tools)
        {
            // context.DotNetCoreTool($"tool update --tool-path {context.Configuration.GetToolPath()} --version {version} {toolName}");
        }
    }
}
