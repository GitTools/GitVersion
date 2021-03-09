using System.Collections.Generic;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.DotNetFormat;

namespace Build.Tasks
{
    [TaskName(nameof(CodeFormat))]
    [TaskDescription("Formats the code")]
    public class CodeFormat : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetFormat(new DotNetCoreFormatSettings
            {
                Workspace = new DirectoryPath(context.Paths.Build),
                Folder = true,
                FixWhitespaces = true,
            });

            context.DotNetFormat(new DotNetCoreFormatSettings
            {
                Workspace = new DirectoryPath(context.Paths.Src),
                Folder = true,
                FixWhitespaces = true,
                Exclude = new List<string> { " **/AddFormats/" }
            });
        }
    }
}
