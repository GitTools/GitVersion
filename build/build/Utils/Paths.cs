namespace Build
{
    public class Paths
    {
        public static string Artifacts => "./artifacts";
        public static string Src => "./src";
        public static string Build => "./build";
        public static string TestOutput => $"{Artifacts}/test-results";
        public static string Packages => $"{Artifacts}/packages";
        public static string Native => $"{Packages}/native";

        public static string Nuget => $"{Packages}/nuget";
        public static string ArtifactsBinCmdline => $"{Packages}/prepare/cmdline";
        public static string ArtifactsBinPortable => $"{Packages}/prepare/portable";
    }
}
