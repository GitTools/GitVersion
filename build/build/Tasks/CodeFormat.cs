using Common.Addins.Cake.DotNetCoreFormat;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(CodeFormat))]
[TaskDescription("Formats the code")]
public class CodeFormat : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Code format...");
        context.DotNetCoreFormat(new DotNetCoreFormatSettings
        {
            Workspace = Paths.Build
        });

        context.DotNetCoreFormat(new DotNetCoreFormatSettings
        {
            Workspace = Paths.Src,
            Exclude = new List<string> { " **/AddFormats/" }
        });
    }
}
