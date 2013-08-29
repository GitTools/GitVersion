namespace GitFlowVersion
{
    public class SemanticVersion
    {
        public int Major;
        public int Minor;
        public int Patch;
        public int PreRelease;
        public Stage Stage;

        public static SemanticVersion FromMajorMinorPatch(string versionString)
        {
            var strings = versionString.Split('.');
            return new SemanticVersion
                   {
                       Major = int.Parse(strings[0]),
                       Minor = int.Parse(strings[1]),
                       Patch = int.Parse(strings[2]),
                   };
        }
        public static bool IsMajorMinorPatch(string versionString)
        {
            var strings = versionString.Split('.');
            if (strings.Length != 3)
            {
                return false;
            }
            int fake;
            if (!int.TryParse(strings[0], out fake))
            {
                return false;
            }
            if (!int.TryParse(strings[1], out fake))
            {
                return false;
            }
            if (!int.TryParse(strings[2], out fake))
            {
                return false;
            }
            return true;
        }
    }
}