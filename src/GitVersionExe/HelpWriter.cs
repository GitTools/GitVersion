namespace GitVersion
{
    using System;
    using System.IO;

    class HelpWriter
    {
        public static void Write()
        {
            WriteTo(Console.WriteLine);
        }

        public static void WriteTo(Action<string> writeAction)
        {
            const string messageHeader = @"Use convention to derive a SemVer product version from a GitFlow or GitHub based repository.

gitversion [path]
gitversion [init]  Configuration utility for gitversion

";
            var options = ArgumentParser.GetOptionSet(new Arguments());

            var sw = new StringWriter();
            options.WriteOptionDescriptions(sw);
            var message = messageHeader + sw;
 
            writeAction(message);
        }
    }
}