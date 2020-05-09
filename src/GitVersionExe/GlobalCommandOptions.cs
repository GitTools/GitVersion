namespace GitVersion
{
    public class GlobalCommandOptions
    {
        public string WorkingDirectory { get; set; } = System.Environment.CurrentDirectory;     
        public LoggingMethod LoggingMethod { get; set; }
        public string LogFilePath { get; set; }
    }
}
