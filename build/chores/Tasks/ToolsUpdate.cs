using Cake.Json;
using Newtonsoft.Json.Linq;

namespace Chores.Tasks;

[TaskName(nameof(ToolsUpdate))]
[TaskDescription("Update dotnet local tools")]
public class ToolsUpdate : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var json = context.ParseJsonFromFile("dotnet-tools.json");
        var jToken = json["tools"];
        if (jToken != null)
        {
            var tools = jToken.Select(x => ((JProperty)x).Name).ToArray();
            foreach (var tool in tools)
            {
                context.DotNetCoreTool($"tool update --local {tool}");
            }
        }
    }
}
