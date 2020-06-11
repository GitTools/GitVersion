using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using Core;

namespace Output
{
    public class OutputCommand : Command
    {
        public OutputCommand(IEnumerable<BaseOutputCommand> outputCommands): base("output", "Outputs the version object.")
        {
            AddGlobalOption(new Option<string>("--output-dir", "The output directory with the git repository"));
            AddGlobalOption(new Option<string>("--input-file", "The input version file"));
            
            foreach (var command in outputCommands)
            {
                AddCommand(command);
            }
        }
    }

    public abstract class BaseOutputCommand : Command
    {
        protected BaseOutputCommand(string name, string description = null) : base(name, description)
        {
        }
    }
    
    public class OutputAssemblyInfoCommand : BaseOutputCommand
    {
        public OutputAssemblyInfoCommand() : base("assemblyinfo", "Outputs version to assembly")
        {
            AddOption(new Option<string>("--assemblyinfo-file", "The assembly file"));
            Handler = CommandHandler.Create<OutputAssemblyInfoOptions>(Output);
        }
        
        private void Output(OutputAssemblyInfoOptions o)
        {
            Console.WriteLine($"Command : 'output assemblyinfo', LogFile : '{o.LogFile}', WorkDir : '{o.OutputDir}', InputFile: '{o.InputFile}', AssemblyInfo: '{o.AssemblyinfoFile}' ");
        }
    }
    
    public class OutputProjectCommand : BaseOutputCommand
    {
        public OutputProjectCommand() : base("project", "Outputs version to project")
        {
            AddOption(new Option<string>("--project-file", "The project file"));
            Handler = CommandHandler.Create<OutputProjectOptions>(Output);
        }
        
        private void Output(OutputProjectOptions o)
        {
            Console.WriteLine($"Command : 'output assemblyinfo', LogFile : '{o.LogFile}', WorkDir : '{o.OutputDir}', InputFile: '{o.InputFile}', Project: '{o.ProjectFile}' ");
        }
    }

    public class OutputAssemblyInfoOptions : OutputOptions
    {
        [Option("--assemblyinfo-file", "The assembly file")]
        public string AssemblyinfoFile { get; set; }
    }
    
    public class OutputProjectOptions : OutputOptions
    {
        [Option("--project-file", "The project file")]
        public string ProjectFile { get; set; }
    }
}