namespace GitFlowVersion
{
    using System;
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

                int preReleaseNumber;
                if (!int.TryParse(prereleaseString.Substring(buildIndex), out preReleaseNumber))
                {
                    semanticVersion = null;
                    return false;
                }
                parsedVersion.PreReleaseNumber = preReleaseNumber;

            }
            semanticVersion = parsedVersion;
            return true;
        }


    }
}