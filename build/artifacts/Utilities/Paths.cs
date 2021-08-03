namespace Artifacts.Utilities
{
    public class Paths
    {
        public static string Artifacts => "./artifacts";
        public static string Packages => $"{Artifacts}/packages";
        public static string Native => $"{Packages}/native";

        public static string Nuget => $"{Packages}/nuget";
    }
}
