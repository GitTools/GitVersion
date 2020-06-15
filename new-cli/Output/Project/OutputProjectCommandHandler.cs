using System;
using System.Threading.Tasks;
using Core;

namespace Output
{
    public class OutputProjectCommandHandler : CommandHandler<OutputProjectOptions>, IOutputCommandHandler
    {
        private readonly IService service;

        public OutputProjectCommandHandler(IService service)
        {
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputProjectOptions options)
        {
            var value = service.Call();
            Console.WriteLine($"Command : 'output project', LogFile : '{options.LogFile}', WorkDir : '{options.OutputDir}', InputFile: '{options.InputFile}', Project: '{options.ProjectFile}' ");
            return Task.FromResult(value);
        }
    }
}