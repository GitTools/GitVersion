namespace GitVersion.Options
{
    using System.Collections.Generic;
    using CommandLine;
    using CommandLine.Text;

    [Verb("inject-buildserver",
        HelpText = "Inject gitversion variables as environment variables in your build server job.")]
    class InjectBuildServerOptions : LoggingOptions
    {
        [Usage(ApplicationAlias = "GitVersion")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal scenario, will detect build server automatically", new InjectBuildServerOptions());
            }
        }
    }
}