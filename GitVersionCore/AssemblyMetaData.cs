namespace GitVersion
{
    public class AssemblyMetaData
    {
        public AssemblyMetaData(string version, string fileVersion)
        {
            Version = version;
            FileVersion = fileVersion;
        }

        public string Version { get; private set; }
        public string FileVersion { get; private set; }
    }
}
