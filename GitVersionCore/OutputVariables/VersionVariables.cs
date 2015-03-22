namespace GitVersion
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class VersionVariables : IEnumerable<KeyValuePair<string, string>>
    {
        public VersionVariables(string major, string minor, string patch, string buildMetaData, string fullBuildMetaData, string branchName, string sha, string majorMinorPatch, string semVer, string legacySemVer, string legacySemVerPadded, string fullSemVer, string assemblySemVer, string preReleaseTag, string preReleaseTagWithDash, string informationalVersion)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            BuildMetaData = buildMetaData;
            FullBuildMetaData = fullBuildMetaData;
            BranchName = branchName;
            Sha = sha;
            MajorMinorPatch = majorMinorPatch;
            SemVer = semVer;
            LegacySemVer = legacySemVer;
            LegacySemVerPadded = legacySemVerPadded;
            FullSemVer = fullSemVer;
            AssemblySemVer = assemblySemVer;
            PreReleaseTag = preReleaseTag;
            PreReleaseTagWithDash = preReleaseTagWithDash;
            InformationalVersion = informationalVersion;
        }

        public string Major { get; private set; }
        public string Minor { get; private set; }
        public string Patch { get; private set; }
        public string PreReleaseTag { get; private set; }
        public string PreReleaseTagWithDash { get; private set; }
        public string BuildMetaData { get; private set; }
        public string FullBuildMetaData { get; private set; }
        public string MajorMinorPatch { get; private set; }
        public string SemVer { get; private set; }
        public string LegacySemVer { get; private set; }
        public string LegacySemVerPadded { get; private set; }
        public string AssemblySemVer { get; private set; }
        public string FullSemVer { get; private set; }
        public string InformationalVersion { get; private set; }
        public string BranchName { get; private set; }
        public string Sha { get; private set; }

        // Synonyms
        // TODO When NuGet 3 is released: public string NuGetVersionV3 { get { return ??; } }
        public string NuGetVersionV2 { get { return LegacySemVerPadded.ToLower(); } }
        public string NuGetVersion { get { return NuGetVersionV2; } }

        public IEnumerable<string> AvailableVariables
        {
            get { return typeof(VersionVariables).GetProperties().Select(p => p.Name).OrderBy(a => a); }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            var type = typeof(string);
            return typeof(VersionVariables)
                .GetProperties()
                .Where(p => p.PropertyType == type && !p.GetIndexParameters().Any())
                .Select(p => new KeyValuePair<string, string>(p.Name, (string) p.GetValue(this, null)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string this [string variable]
        {
            get { return (string) typeof(VersionVariables).GetProperty(variable).GetValue(this, null); }
        }

        public bool TryGetValue(string variable, out string variableValue)
        {
            if (ContainsKey(variable))
            {
                variableValue = this[variable];
                return true;
            }

            variableValue = null;
            return false;
        }

        bool ContainsKey(string variable)
        {
            return typeof(VersionVariables).GetProperty(variable) != null;
        }
    }
}