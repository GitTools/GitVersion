namespace GitVersion.Options
{
    using System.Collections.Generic;
    using CommandLine;
    using CommandLine.Text;

    [Verb("inject-assembly-info",
        HelpText = "Search for all assembly information files in the git repo and update them using GitVersion variables.")]
    class InjectAssemblyInfo : LoggingOptions
    {
        [Option('f', "filename",
            HelpText = "Assembly information filename", Default = "AssemblyInfo.cs")]
        public string AssemblyInformationFileName { get; set; }

        [Option('p', "path",
            HelpText = "The path to inspect, defaults to current directory."
            )]
        public string Path { get; set; }

        [Usage(ApplicationAlias = "GitVersion")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Search all AssemblyInfo.cs and update with GitVersion variable information", new InjectAssemblyInfo());
            }
        }
    
    }
}