namespace GitVersion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using GitVersion.Helpers;
    using System.Reflection;
 

    using YamlDotNet.Serialization;

    public class VersionVariables : IEnumerable<KeyValuePair<string, string>>
    {
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
                                string assemblySemFileVer,
                                string preReleaseTag,
                                string preReleaseTagWithDash,
                                string preReleaseLabel,
                                string preReleaseNumber,
                                string informationalVersion,
                                string commitDate,
                                string nugetVersion,
                                string nugetVersionV2,
                                string nugetPreReleaseTag,
                                string nugetPreReleaseTagV2,
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
            AssemblySemFileVer = assemblySemFileVer;
            PreReleaseTag = preReleaseTag;
            PreReleaseTagWithDash = preReleaseTagWithDash;
            PreReleaseLabel = preReleaseLabel;
            PreReleaseNumber = preReleaseNumber;
            InformationalVersion = informationalVersion;
            CommitDate = commitDate;
            NuGetVersion = nugetVersion;
            NuGetVersionV2 = nugetVersionV2;
            NuGetPreReleaseTag = nugetPreReleaseTag;
            NuGetPreReleaseTagV2 = nugetPreReleaseTagV2;
            CommitsSinceVersionSource = commitsSinceVersionSource;
            CommitsSinceVersionSourcePadded = commitsSinceVersionSourcePadded;
        }

        public string Major { get; private set; }
        public string Minor { get; private set; }
        public string Patch { get; private set; }
        public string PreReleaseTag { get; private set; }
        public string PreReleaseTagWithDash { get; private set; }
        public string PreReleaseLabel { get; private set; }
        public string PreReleaseNumber { get; private set; }
        public string BuildMetaData { get; private set; }
        public string BuildMetaDataPadded { get; private set; }
        public string FullBuildMetaData { get; private set; }
        public string MajorMinorPatch { get; private set; }
        public string SemVer { get; private set; }
        public string LegacySemVer { get; private set; }
        public string LegacySemVerPadded { get; private set; }
        public string AssemblySemVer { get; private set; }
        public string AssemblySemFileVer { get; private set; }
        public string FullSemVer { get; private set; }
        public string InformationalVersion { get; private set; }
        public string BranchName { get; private set; }
        public string Sha { get; private set; }
        public string NuGetVersionV2 { get; private set; }
        public string NuGetVersion { get; private set; }
        public string NuGetPreReleaseTagV2 { get; private set; }
        public string NuGetPreReleaseTag { get; private set; }
        public string CommitsSinceVersionSource { get; private set; }
        public string CommitsSinceVersionSourcePadded { get; private set; }

        [ReflectionIgnore]
        public static IEnumerable<string> AvailableVariables
        {
            get
            {
                return typeof(VersionVariables)
                    .GetProperties()
                    .Where(p => !p.GetCustomAttributes(typeof(ReflectionIgnoreAttribute), false).Any())
                    .Select(p => p.Name)
                    .OrderBy(a => a);
            }
        }

        public string CommitDate { get; set; }

        [ReflectionIgnore]
        public string FileName { get; set; }

        [ReflectionIgnore]
        public string this[string variable]
        {


            get
            {
#if NETDESKTOP
                return typeof(VersionVariables).GetProperty(variable).GetValue(this, null) as string;
#else
                throw new NotImplementedException();
                //  return typeof(VersionVariables).GetTypeInfo().GetProperty(variable).GetValue(this, null) as string;
#endif



            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            var type = typeof(string);
            return typeof(VersionVariables)
                .GetProperties()
                .Where(p => p.PropertyType == type && !p.GetIndexParameters().Any() && !p.GetCustomAttributes(typeof(ReflectionIgnoreAttribute), false).Any())
                .Select(p => new KeyValuePair<string, string>(p.Name, (string)p.GetValue(this, null)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static VersionVariables FromDictionary(IEnumerable<KeyValuePair<string, string>> properties)
        {
            var type = typeof(VersionVariables);
            var constructors = type.GetConstructors();

            var ctor = constructors.Single();
            var ctorArgs = ctor.GetParameters()
                .Select(p => properties.Single(v => string.Equals(v.Key, p.Name, StringComparison.CurrentCultureIgnoreCase)).Value)
                .Cast<object>()
                .ToArray();
            return (VersionVariables)Activator.CreateInstance(type, ctorArgs);
        }

        public static VersionVariables FromFile(string filePath, IFileSystem fileSystem)
        {
            using (var stream = fileSystem.OpenRead(filePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var dictionary = new Deserializer().Deserialize<Dictionary<string, string>>(reader);
                    var versionVariables = FromDictionary(dictionary);
                    versionVariables.FileName = filePath;
                    return versionVariables;
                }
            }
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
#if NETDESKTOP
            return typeof(VersionVariables).GetProperty(variable) != null;
#else
            throw new NotImplementedException();
            // return typeof(VersionVariables).GetTypeInfo().GetProperty(variable) != null;
#endif
        }

        sealed class ReflectionIgnoreAttribute : Attribute
        {
        }
    }
}