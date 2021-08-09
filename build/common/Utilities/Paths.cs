using Cake.Core.IO;

namespace Common.Utilities
{
    public class Paths
    {
        public static readonly DirectoryPath Root = "./";

        public static readonly DirectoryPath ToolsDirectory = $"{Root}tools";
        public static readonly DirectoryPath Artifacts = $"{Root}artifacts";
        public static readonly DirectoryPath Src = $"{Root}src";
        public static readonly DirectoryPath Docs = $"{Root}docs";
        public static readonly DirectoryPath Build = $"{Root}build";

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
