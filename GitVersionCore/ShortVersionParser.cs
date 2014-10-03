namespace GitVersion
{
    using System;

    class ShortVersionParser
    {
        public static void Parse(string versionString, out ShortVersion shortVersion)
        {
            if (!TryParse(versionString, out shortVersion))
            {
                throw new Exception(string.Format("Could not parse version from '{0}' expected 'major.minor.patch'", versionString));
            }
        }

        public static bool TryParseMajorMinor(string versionString, out ShortVersion shortVersion)
        {
            if (!TryParse(versionString, out shortVersion))
            {
                return false;
            }

            // Note: during scanning of master we only want the last major / minor, not the patch, so patch must be zero
            return shortVersion.Patch == 0;
        }

        public static bool TryParse(string versionString, out ShortVersion shortVersion)
        {
            var major = 0;
            var minor = 0;
            var patch = 0;
            var strings = versionString.Split('.');
            if (strings.Length < 2 || strings.Length > 3)
            {
                shortVersion = null;
                return false;
            }
            if (!int.TryParse(strings[0], out major))
            {
                shortVersion = null;
                return false;
            }

            if (!int.TryParse(strings[1], out minor))
            {
                shortVersion = null;
                return false;
            }

            if (strings.Length == 3)
            {
                if (!int.TryParse(strings[2], out patch))
                {
                    shortVersion = null;
                    return false;
                }
            }

            shortVersion = new ShortVersion{Major = major,Minor = minor,Patch = patch};
            return true;
        }
    }
}