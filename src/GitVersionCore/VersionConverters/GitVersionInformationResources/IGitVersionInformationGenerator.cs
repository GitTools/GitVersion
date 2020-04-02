using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.GitVersionInformationResources
{
    public interface IGitVersionInformationGenerator
    {
        void Generate(VersionVariables variables, FileWriteInfo writeInfo);
    }
}
