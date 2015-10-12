namespace GitVersion.Options
{
    using System.Collections.Generic;
    using CommandLine;
    using CommandLine.Text;

    [Verb("inject-msbuild",
        HelpText = "Build an msbuild file, GitVersion variables will be injected as msbuild properties.")]
    class InjectMsBuildOptions : LoggingOptions
    {
        [Option('f',"filename", HelpText = "MS build file to build.")]
        public string BuildFileName { get; set; }

        [Usage(ApplicationAlias = "GitVersion")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Build msbuild file", new InjectMsBuildOptions { BuildFileName = "GitVersion.sln" });
            }
        }
    
    }
}