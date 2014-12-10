namespace GitVersion
{
    using System.IO;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class ConfigReader
    {
        public static Config Read(TextReader reader)
        {
            var deserializer = new Deserializer(null, new HyphenatedNamingConvention());
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
            writer.WriteLine("# tag-prefix: '[vV|version-] # regex to match git tag prefix");
            writer.WriteLine("# next-version: 1.0.0");
            writer.WriteLine("# mode: ContinuousDelivery | ContinuousDeployment");
            writer.WriteLine("# release*:\n    mode: ContinuousDelivery | ContinuousDeployment\n    tag: rc");
            writer.WriteLine("# develop:\n    mode: ContinuousDelivery | ContinuousDeployment\n    tag: alpha");
        }
    }
}