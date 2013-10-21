namespace GitFlowVersion
{
    using System;

    public class ShortVersionParser
    {

        public static void Parse(string versionString, out int major, out int minor, out int patch)
        {
            var strings = versionString.Split('.');
            if (strings.Length != 3)
            {
                Throw(versionString);
            }
            if (!int.TryParse(strings[0], out major))
            {
                Throw(versionString);
            }

            if (!int.TryParse(strings[1], out minor))
            {
                Throw(versionString);
            }

            if (!int.TryParse(strings[2], out patch))
            {
                Throw(versionString);
            }

        }
        public static bool TryParseMajorMinor(string versionString, out int major, out int minor)
        {
            major = 0;
            minor = 0;
            int patch;
            var strings = versionString.Split('.');
            if (strings.Length != 3)
            {
                return false;
            }
            if (!int.TryParse(strings[0], out major))
            {
                return false;
            }

            if (!int.TryParse(strings[1], out minor))
            {
                return false;
            }

            if (!int.TryParse(strings[2], out patch))
            {
                return false;
            }

            return patch == 0;
        }
        public static bool TryParse(string versionString, out int major, out int minor, out int patch)
        {
            major = 0;
            minor = 0;
            patch = 0;
            var strings = versionString.Split('.');
            if (strings.Length != 3)
            {
                return false;
            }
            if (!int.TryParse(strings[0], out major))
            {
                return false;
            }

            if (!int.TryParse(strings[1], out minor))
            {
                return false;
            }

            if (!int.TryParse(strings[2], out patch))
            {
                return false;
            }

            return true;
        }

        static void Throw(string versionString)
        {
            throw new Exception(string.Format("Could not parse version from '{0}' expected 'major.minor.patch'", versionString));
        }
    }
}