namespace GitVersion
{
    using System.Linq;

    public class SemanticVersionParser
    {
        public static bool TryParse(string versionString, out SemanticVersion semanticVersion)
        {
            var parts = versionString.Split('-');
            if (parts.Length > 2)
            {
                semanticVersion = null;
                return false;
            }
            var stableParts = parts.First().Split('.');

            if (stableParts.Length > 3)
            {
                semanticVersion = null;
                return false;
            }

            int major;
            if (!int.TryParse(stableParts[0], out major))
            {
                semanticVersion = null;
                return false;
            }
            var parsedVersion = new SemanticVersion
                                {
                                    Major = major
                                };
            if (stableParts.Length > 1)
            {
                int minor;
                if (!int.TryParse(stableParts[1], out minor))
                {
                    semanticVersion = null;
                    return false;
                }
                parsedVersion.Minor = minor;
            }

            if (stableParts.Length > 2)
            {
                int patch;
                if (!int.TryParse(stableParts[2], out patch))
                {
                    semanticVersion = null;
                    return false;
                }
                parsedVersion.Patch = patch;
            }

            if (parts.Length > 1)
            {
                var preReleaseString = parts[1];
                var preReleaseParts = preReleaseString.Split('.');
                if (preReleaseParts.Length > 2)
                {
                    semanticVersion = null;
                    return false;
                }

                parsedVersion.PreReleaseTag = preReleaseParts[0];

                if ((preReleaseParts.Length > 1))
                {
                    int preReleasePartTwo;
                    if ((!int.TryParse(preReleaseParts[1], out preReleasePartTwo)))
                    {
                        semanticVersion = null;
                        return false;
                    }

                    parsedVersion.PreReleasePartTwo = preReleasePartTwo;
                }
            }

            semanticVersion = parsedVersion;
            return true;
        }

        public static SemanticVersion Parse(string versionString)
        {
            SemanticVersion parsedVersion;
            if (TryParse(versionString, out parsedVersion))
            {
                return parsedVersion;
            }

            throw new ErrorException(string.Format("Could not parse version from '{0}' expected 'Major.Minor.Patch[-StabilityPreRealeasePartOne[.PreRealeasePartTwo]]'", versionString));
        }
    }
}
