using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion
{
    public class ConfigReader
    {
        public static Config Read(TextReader reader)
        {
            var deserializer = new Deserializer(null, new CamelCaseNamingConvention());
            var deserialize = deserializer.Deserialize<Config>(reader);
            if (deserialize == null)
            {
                return new Config();
            }
            return deserialize;
        }

        public static void WriteSample(TextWriter writer)
        {
            writer.WriteLine("# assembly-versioning-scheme: MajorMinorPatchMetadata | MajorMinorPatch | MajorMinor | Major");
            writer.WriteLine("# develop-branch-tag: alpha");
            writer.WriteLine("# release-branch-tag: rc");
            writer.WriteLine("# tag-prefix: '[vV|version-] # regex to match git tag prefix");
            writer.WriteLine("# next-version: 1.0.0");
        }
    }
}