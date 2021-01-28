using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using YamlDotNet.Serialization;
using static GitVersion.Extensions.ObjectExtensions;

namespace GitVersion.OutputVariables
{
    public class VersionVariables : IEnumerable<KeyValuePair<string, string>>
    {
        public VersionVariables(string major,
            string minor,
            string patch,
            string buildMetaData,
            string buildMetaDataPadded,
            string fullBuildMetaData,
            string branchName,
            string escapedBranchName,
            string sha,
            string shortSha,
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
            string preReleaseLabelWithDash,
            string preReleaseNumber,
            string weightedPreReleaseNumber,
            string informationalVersion,
            string commitDate,
            string nugetVersion,
            string nugetVersionV2,
            string nugetPreReleaseTag,
            string nugetPreReleaseTagV2,
            string versionSourceSha,
            string commitsSinceVersionSource,
            string commitsSinceVersionSourcePadded,
            string uncommittedChanges)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            BuildMetaData = buildMetaData;
            BuildMetaDataPadded = buildMetaDataPadded;
            FullBuildMetaData = fullBuildMetaData;
            BranchName = branchName;
            EscapedBranchName = escapedBranchName;
            Sha = sha;
            ShortSha = shortSha;
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
            PreReleaseLabelWithDash = preReleaseLabelWithDash;
            PreReleaseNumber = preReleaseNumber;
            WeightedPreReleaseNumber = weightedPreReleaseNumber;
            InformationalVersion = informationalVersion;
            CommitDate = commitDate;
            NuGetVersion = nugetVersion;
            NuGetVersionV2 = nugetVersionV2;
            NuGetPreReleaseTag = nugetPreReleaseTag;
            NuGetPreReleaseTagV2 = nugetPreReleaseTagV2;
            VersionSourceSha = versionSourceSha;
            CommitsSinceVersionSource = commitsSinceVersionSource;
            CommitsSinceVersionSourcePadded = commitsSinceVersionSourcePadded;
            UncommittedChanges = uncommittedChanges;
        }

        public string Major { get; }
        public string Minor { get; }
        public string Patch { get; }
        public string PreReleaseTag { get; }
        public string PreReleaseTagWithDash { get; }
        public string PreReleaseLabel { get; }
        public string PreReleaseLabelWithDash { get; }
        public string PreReleaseNumber { get; }
        public string WeightedPreReleaseNumber { get; }
        public string BuildMetaData { get; }
        public string BuildMetaDataPadded { get; }
        public string FullBuildMetaData { get; }
        public string MajorMinorPatch { get; }
        public string SemVer { get; }
        public string LegacySemVer { get; }
        public string LegacySemVerPadded { get; }
        public string AssemblySemVer { get; }
        public string AssemblySemFileVer { get; }
        public string FullSemVer { get; }
        public string InformationalVersion { get; }
        public string BranchName { get; }
        public string EscapedBranchName { get; }
        public string Sha { get; }
        public string ShortSha { get; }
        public string NuGetVersionV2 { get; }
        public string NuGetVersion { get; }
        public string NuGetPreReleaseTagV2 { get; }
        public string NuGetPreReleaseTag { get; }
        public string VersionSourceSha { get; }
        public string CommitsSinceVersionSource { get; }
        public string CommitsSinceVersionSourcePadded { get; }

        public string UncommittedChanges { get; }

        [ReflectionIgnore]
        public static IEnumerable<string> AvailableVariables
        {
            get
            {
                return typeof(VersionVariables)
                    .GetProperties()
                    .Where(p => !p.GetCustomAttributes(typeof(ReflectionIgnoreAttribute), false).Any())
                    .Select(p => p.Name)
                    .OrderBy(a => a, StringComparer.Ordinal);
            }
        }

        public string CommitDate { get; set; }

        [ReflectionIgnore]
        public string FileName { get; set; }

        [ReflectionIgnore]
        public string this[string variable] => typeof(VersionVariables).GetProperty(variable)?.GetValue(this, null) as string;

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return this.GetProperties().GetEnumerator();
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
                .Select(p => properties.Single(v => string.Equals(v.Key, p.Name, StringComparison.InvariantCultureIgnoreCase)).Value)
                .Cast<object>()
                .ToArray();
            return (VersionVariables)Activator.CreateInstance(type, ctorArgs);
        }

        public static VersionVariables FromJson(string json)
        {
            var serializeOptions = JsonSerializerOptions();
            var variablePairs = JsonSerializer.Deserialize<Dictionary<string, string>>(json, serializeOptions);
            return FromDictionary(variablePairs);
        }

        public static VersionVariables FromFile(string filePath, IFileSystem fileSystem)
        {
            using var stream = fileSystem.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            var dictionary = new Deserializer().Deserialize<Dictionary<string, string>>(reader);
            var versionVariables = FromDictionary(dictionary);
            versionVariables.FileName = filePath;
            return versionVariables;
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

        private static bool ContainsKey(string variable)
        {
            return typeof(VersionVariables).GetProperty(variable) != null;
        }

        public override string ToString()
        {
            var variables = this.GetProperties().ToDictionary(x => x.Key, x => x.Value);
            var serializeOptions = JsonSerializerOptions();

            return JsonSerializer.Serialize(variables, serializeOptions);
        }

        private static JsonSerializerOptions JsonSerializerOptions()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new GitVersionStringConverter() }
            };
            return serializeOptions;
        }
    }

    public class GitVersionStringConverter : JsonConverter<string>
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(string);

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number && typeToConvert == typeof(string))
                return reader.GetString() ?? "";

            var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out long number, out var bytesConsumed) && span.Length == bytesConsumed)
                return number.ToString();

            var data = reader.GetString();

            throw new InvalidOperationException($"'{data}' is not a correct expected value!")
            {
                Source = nameof(GitVersionStringConverter)
            };
        }

        public override void Write(Utf8JsonWriter writer, [CanBeNull] string value, JsonSerializerOptions options)
        {
            value ??= string.Empty;
            if (NotAPaddedNumber(value) && int.TryParse(value, out var number))
                writer.WriteNumberValue(number);
            else
                writer.WriteStringValue(value);
        }

        public override bool HandleNull => true;

        private static bool NotAPaddedNumber(string value) => value != null && (value == "0" || !value.StartsWith("0"));
    }
}
