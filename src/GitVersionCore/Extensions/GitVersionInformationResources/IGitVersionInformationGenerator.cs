using GitVersion.OutputVariables;

namespace GitVersion.Extensions.GitVersionInformationResources
{
    public interface IGitVersionInformationGenerator
    {
        void Generate(VersionVariables variables, FileWriteInfo writeInfo);
    }
}
