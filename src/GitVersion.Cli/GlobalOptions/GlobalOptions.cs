using System;

namespace GitVersion.Cli
{  

    public class GlobalOptions
    {   

        public GlobalOptions(LoggingOptions logTo = null)
        {
            LogTo = logTo;
        }

        public string WorkingDirectory { get; set; } = System.Environment.CurrentDirectory;      

        public LoggingOptions LogTo { get; set; }
    }

}
