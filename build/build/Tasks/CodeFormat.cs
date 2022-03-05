using Cake.Common.Tools.DotNet.Format;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(CodeFormat))]
[TaskDescription("Formats the code")]
public class CodeFormat : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Code format...");
        context.DotNetFormat(Paths.Build.FullPath);
        context.DotNetFormat(Paths.Src.FullPath, new DotNetFormatSettings
        {
            Exclude = new List<string> { " **/AddFormats/" }
        });
    }
}
