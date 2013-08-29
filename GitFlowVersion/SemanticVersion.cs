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
            var fromMajorMinorPatch = new SemanticVersion
                                      {
                                          Major = int.Parse(strings[0]),
                                      };
            if (strings.Length > 1)
            {
                fromMajorMinorPatch.Minor = int.Parse(strings[1]);
            }
            if (strings.Length > 2)
            {
                fromMajorMinorPatch.Patch = int.Parse(strings[2]);
            }
            return fromMajorMinorPatch;
        }
        public static bool IsVersion(string versionString)
        {
            var strings = versionString.Split('.');
            if (strings.Length > 3)
            {
                return false;
            }
            int fake;
            if (strings.Length > 0 && !int.TryParse(strings[0], out fake))
            {
                return false;
            }
            if (strings.Length > 1 && !int.TryParse(strings[1], out fake))
            {
                return false;
            }
            if (strings.Length > 2 && !int.TryParse(strings[2], out fake))
            {
                return false;
            }
            return true;
        }
    }
}