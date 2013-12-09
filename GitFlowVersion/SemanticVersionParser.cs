namespace GitFlowVersion
{
    using System;
    using System.Linq;

    public class SemanticVersionParser
    {
        public static bool TryParse(string versionString, out SemanticVersion semanticVersion)
        {
            return TryParse(versionString, out semanticVersion, true);
        }

        public static bool TryParse(string versionString, out SemanticVersion semanticVersion, bool normalize)
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
                var prereleaseString = parts[1];

                var buildIndex = prereleaseString.IndexOfAny("0123456789".ToCharArray());
                if (buildIndex < 0)
                {
                    semanticVersion = null;
                    return false;
                }
                var stageString = prereleaseString.Substring(0, buildIndex);

                if (stageString == "RC")
                {
                    parsedVersion.Stability = Stability.ReleaseCandidate;    
                }
                else
                {
                    Stability stability;
                    if (!Enum.TryParse(stageString, true, out stability))
                    {
                        semanticVersion = null;
                        return false;
                    }
                    parsedVersion.Stability = stability;    
                }

                int preReleasePartOne;
                var preReleaseString = prereleaseString.Substring(buildIndex);
                var preReleaseParts = preReleaseString.Split('.');
                if (preReleaseParts.Length > 2)
                {

                    semanticVersion = null;
                    return false;
                }
                if (!int.TryParse(preReleaseParts[0], out preReleasePartOne))
                {
                    semanticVersion = null;
                    return false;
                }
                parsedVersion.PreReleasePartOne = preReleasePartOne;

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
            else
            {
                if (normalize)
                {
                    parsedVersion.Stability = Stability.Final;
                }
            }
            semanticVersion = parsedVersion;
            return true;
        }

        public static SemanticVersion Parse(string versionString, bool normalize)
        {
            SemanticVersion parsedVersion;
            if (TryParse(versionString, out parsedVersion, normalize))
            {
                return parsedVersion;
            }

            throw new ErrorException(string.Format("Could not parse version from '{0}' expected 'Major.Minor.Patch[-StabilityPreRealeasePartOne[.PreRealeasePartTwo]]'", versionString));
        }
    }
}
