using Cake.Core.IO;

namespace Common.Utilities
{
    public class Paths
    {
        public static readonly DirectoryPath ToolsDirectory = "./tools";
        public static readonly DirectoryPath Artifacts = "./artifacts";
        public static readonly DirectoryPath Src = "./src";
        public static readonly DirectoryPath Docs = "./docs";
        public static readonly DirectoryPath Build = "./build";

        public static readonly DirectoryPath Nuspec = $"{Build}/nuspec";

        public static readonly DirectoryPath TestOutput = $"{Artifacts}/test-results";
        public static readonly DirectoryPath Packages = $"{Artifacts}/packages";
        public static readonly DirectoryPath ArtifactsDocs = $"{Artifacts}/docs";
        public static readonly DirectoryPath Native = $"{Packages}/native";

        public static readonly DirectoryPath Nuget = $"{Packages}/nuget";
        public static readonly DirectoryPath ArtifactsBinCmdline = $"{Packages}/prepare/cmdline";
        public static readonly DirectoryPath ArtifactsBinPortable = $"{Packages}/prepare/portable";
    }
}
