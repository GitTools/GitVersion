namespace GitVersion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class VersionVariables : IEnumerable<KeyValuePair<string, string>>
    {
        public static VersionVariables FromDictionary(IEnumerable<KeyValuePair<string, string>> properties)
        {
            var type = typeof(VersionVariables);
            var ctor = type.GetConstructors().Single();
            var ctorArgs = ctor.GetParameters()
                .Select(p => properties.Single(v => v.Key.ToLower() == p.Name.ToLower()).Value)
                .Cast<object>()
                .ToArray();
            return (VersionVariables)Activator.CreateInstance(type, ctorArgs);
        }

        public VersionVariables(string major,
                                string minor,
                                string patch,
                                string buildMetaData,
                                string buildMetaDataPadded,
                                string fullBuildMetaData,
                                string branchName,
                                string sha,
                                string majorMinorPatch,
                                string semVer,
                                string legacySemVer,
                                string legacySemVerPadded,
                                string fullSemVer,
                                string assemblySemVer,
                                string preReleaseTag,
                                string preReleaseTagWithDash,
                                string informationalVersion,
                                string commitDate,
                                string nugetVersion,
                                string nugetVersionV2,
                                string commitsSinceVersionSource,
                                string commitsSinceVersionSourcePadded)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            BuildMetaData = buildMetaData;
            BuildMetaDataPadded = buildMetaDataPadded;
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
            CommitDate = commitDate;
            NuGetVersion = nugetVersion;
            NuGetVersionV2 = nugetVersionV2;
            CommitsSinceVersionSource = commitsSinceVersionSource;
            CommitsSinceVersionSourcePadded = commitsSinceVersionSourcePadded;
        }


        public string Major { get; private set; }
        public string Minor { get; private set; }
        public string Patch { get; private set; }
        public string PreReleaseTag { get; private set; }
        public string PreReleaseTagWithDash { get; private set; }
        public string BuildMetaData { get; private set; }
        public string BuildMetaDataPadded { get; private set; }
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
        public string NuGetVersionV2 { get; private set; }
        public string NuGetVersion { get; private set; }
        public string CommitsSinceVersionSource { get; private set; }
        public string CommitsSinceVersionSourcePadded { get; private set; }

        public static IEnumerable<string> AvailableVariables
        {
            get
            {
                return typeof(VersionVariables)
                    .GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                    .Select(p => p.Name)
                    .Where(p => p != "AvailableVariables" && p != "Item")
                    .OrderBy(a => a);
            }
        }

        public string CommitDate { get; set; }

        public string this[string variable]
        {
            get { return (string)typeof(VersionVariables).GetProperty(variable).GetValue(this, null); }
        }


        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            var type = typeof(string);
            return typeof(VersionVariables)
                .GetProperties()
                .Where(p => p.PropertyType == type && !p.GetIndexParameters().Any())
                .Select(p => new KeyValuePair<string, string>(p.Name, (string)p.GetValue(this, null)))
                .GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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


        public bool ContainsKey(string variable)
        {
            return typeof(VersionVariables).GetProperty(variable) != null;
        }
    }
}