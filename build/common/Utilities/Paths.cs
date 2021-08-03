using Cake.Core.IO;

namespace Common.Utilities
{
    public class Paths
    {
        public static DirectoryPath ToolsDirectory = "./tools";

        public static DirectoryPath Artifacts => "./artifacts";
        public static DirectoryPath Src => "./src";
        public static DirectoryPath Build => "./build";
        public static DirectoryPath TestOutput => $"{Artifacts}/test-results";
        public static DirectoryPath Packages => $"{Artifacts}/packages";
        public static DirectoryPath Native => $"{Packages}/native";

        public static DirectoryPath Nuget => $"{Packages}/nuget";
        public static DirectoryPath ArtifactsBinCmdline => $"{Packages}/prepare/cmdline";
        public static DirectoryPath ArtifactsBinPortable => $"{Packages}/prepare/portable";

        public static DirectoryPath ArtifactsTestBinPortable => $"{Packages}/test/portable";
    }
}
