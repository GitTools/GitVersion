using Common.Addins.Cake.DotNetFormat;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(CodeFormat))]
[TaskDescription("Formats the code")]
public class CodeFormat : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Code format...");
        context.DotNetFormat(new DotNetFormatSettings
        {
            Workspace = Paths.Build
        });

        context.DotNetFormat(new DotNetFormatSettings
        {
            Workspace = Paths.Src,
            Exclude = new List<string> { " **/AddFormats/" }
        });
    }
}
