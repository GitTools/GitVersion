namespace GitFlowVersion
{
    using System;
    using System.Linq;

    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public int Major;
        public int Minor;
        public int Patch;
        public int PreRelease;
        public Stage Stage;

        public string Suffix;

        public static SemanticVersion FromMajorMinorPatch(string versionString)
        {
            var parts = versionString.Split('-');

            var stableParts = parts.First().Split('.');

            var parsedVersion = new SemanticVersion
                                      {
                                          Major = int.Parse(stableParts[0]),
                                      };
            if (stableParts.Length > 1)
            {
                parsedVersion.Minor = int.Parse(stableParts[1]);
            }
            
            if (stableParts.Length > 2)
            {
                parsedVersion.Patch = int.Parse(stableParts[2]);
            }

            if (parts.Length > 1)
            {
                var prereleaseString = parts[1];

                var buildIndex = prereleaseString.IndexOfAny("0123456789".ToCharArray());
                var stageString = prereleaseString.Substring(0, buildIndex);

                if (stageString == "RC")
                    stageString = "ReleaseCandidate";


                parsedVersion.Stage = (Stage) Enum.Parse(typeof (Stage), stageString,ignoreCase:true);
                parsedVersion.PreRelease = int.Parse(prereleaseString.Substring(buildIndex));

            }
            else
            {
                parsedVersion.Stage = Stage.Final;
                parsedVersion.PreRelease = 0;
            }

            return parsedVersion;
        }

        public static bool IsVersion(string versionString)
        {
            var parts = versionString.Split('-');
            var stableVersion = parts.First();
            var stableParts = stableVersion.Split('.');
            
            if (stableParts.Length > 3)
            {
                return false;
            }
            
            int fake;
            if (stableParts.Length > 0 && !int.TryParse(stableParts[0], out fake))
            {
                return false;
            }
            
            if (stableParts.Length > 1 && !int.TryParse(stableParts[1], out fake))
            {
                return false;
            }
            
            if (stableParts.Length > 2 && !int.TryParse(stableParts[2], out fake))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}-{3}{4}", Major, Minor, Patch, Stage, PreRelease);
        }

        //TODO: add order by unit tests
        public int CompareTo(SemanticVersion value)
        {
            if (value == null)
            {
                return 1;
            }
            if (Major != value.Major)
            {
                if (Major > value.Major)
                {
                    return 1;
                }
                return -1;
            }
            if (Minor != value.Minor)
            {
                if (Minor > value.Minor)
                {
                    return 1;
                }
                return -1;
            }
            if (Patch != value.Patch)
            {
                if (Patch > value.Patch)
                {
                    return 1;
                }
                return -1;
            }
            return -1;
        }



    }
}