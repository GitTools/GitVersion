namespace GitVersion.Options
{
    using CommandLine;

    [Verb("update-assembly-info")]
    class UpdateAssemblyInfo : LoggingOptions
    {
        [Option('f', "filename",
            HelpText = "Assembly information filename, defaults to 'AssemblyInfo.cs'.")]
        public string AssemblyInformationFileName { get; set; }
    }
}