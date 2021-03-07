using System.Collections.Generic;
using Cake.Core;
using Cake.Core.IO;
using Cake.DotNetFormat;
using Cake.Frosting;

namespace GitVersion.Build.Tasks
{
    [TaskName(nameof(CodeFormat))]
    [TaskDescription("Formats the code")]
    public class CodeFormat : FrostingTask
    {
        public override void Run(ICakeContext context)
        {
            context.DotNetFormat(new DotNetCoreFormatSettings
            {
                Workspace = new DirectoryPath("./build"),
                Folder = true,
                FixWhitespaces = true,
            });

            context.DotNetFormat(new DotNetCoreFormatSettings
            {
                Workspace = new DirectoryPath("./src"),
                Folder = true,
                FixWhitespaces = true,
                Exclude = new List<string> { " **/AddFormats/" }
            });
        }
    }
}
