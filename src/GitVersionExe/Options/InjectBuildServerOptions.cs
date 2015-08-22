namespace GitVersion.Options
{
    using System.Collections.Generic;
    using CommandLine;
    using CommandLine.Text;

    [Verb("inject-buildserver",
        HelpText = "Inject gitversion variables as environment variables in your build server job.")]
    class InjectBuildServerOptions : LoggingOptions
    {
        [Option(HelpText = "Autodetect the build server, defaults to true.", Default = true)]
        public bool AutoDetect { get; set; }

        [Option(HelpText = "The name of the buildserver to use in case auto-detect is false. " +
                           "One of TeamCity, AppVeyor, ContinuaCi, MyGet, VsoBuild, Jenkins.")]
        public string BuildServerName { get; set; }


        [Usage(ApplicationAlias = "GitVersion")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal scenario, will detect build server automatically", new InjectBuildServerOptions());
                yield return new Example("Specific build server, will run the specified build server integration", 
                      UnParserSettings.WithGroupSwitchesOnly(),
                    new InjectBuildServerOptions { AutoDetect = false, BuildServerName = "Jenkins" });
            }
        }
    }
}