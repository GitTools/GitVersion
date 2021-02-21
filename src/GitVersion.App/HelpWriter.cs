using System;
using System.IO;
using System.Reflection;
using GitVersion.Logging;

namespace GitVersion
{
    public class HelpWriter : IHelpWriter
    {
        private readonly IVersionWriter versionWriter;
        private readonly IConsole console;

        public HelpWriter(IVersionWriter versionWriter, IConsole console)
        {
            this.versionWriter = versionWriter ?? throw new ArgumentNullException(nameof(versionWriter));
            this.console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public void Write()
        {
            WriteTo(console.WriteLine);
        }

        public void WriteTo(Action<string> writeAction)
        {
            var version = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();
            versionWriter.WriteTo(assembly, v => version = v);

            using var argumentsMarkdownStream = GetType().Assembly.GetManifestResourceStream("GitVersion.App.arguments.md");
            using var sr = new StreamReader(argumentsMarkdownStream);
            var argsMarkdown = sr.ReadToEnd();
            var codeBlockStart = argsMarkdown.IndexOf("```");
            var codeBlockEnd = argsMarkdown.LastIndexOf("```");
            argsMarkdown = argsMarkdown.Substring(codeBlockStart + 3, codeBlockEnd).Trim();
            var nl = System.Environment.NewLine;
            var message = "GitVersion "
                        + version + nl
                        + "Use convention to derive a SemVer product version from a GitFlow or GitHub based repository."
                        + nl + nl + "GitVersion [path]"
                        + nl + nl + argsMarkdown;

            writeAction(message);
        }
    }
}
