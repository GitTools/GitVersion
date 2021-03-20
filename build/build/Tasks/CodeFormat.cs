using System.Collections.Generic;
using Cake.Common.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.DotNetCoreFormat;

namespace Build.Tasks
{
    [TaskName(nameof(CodeFormat))]
    [TaskDescription("Formats the code")]
    public class CodeFormat : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.Information("Code format...");
            context.DotNetCoreFormat(new DotNetCoreFormatSettings
            {
                Workspace = new DirectoryPath(context.Paths.Build),
                Folder = true,
                FixWhitespaces = true,
            });

            context.DotNetCoreFormat(new DotNetCoreFormatSettings
            {
                Workspace = new DirectoryPath(context.Paths.Src),
                Folder = true,
                FixWhitespaces = true,
                Exclude = new List<string> { " **/AddFormats/" }
            });
        }
    }
}
