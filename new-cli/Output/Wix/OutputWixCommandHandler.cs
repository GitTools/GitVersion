using System;
using System.Threading.Tasks;
using Core;

namespace Output
{
    public class OutputWixCommandHandler : CommandHandler<OutputWixOptions>, IOutputCommandHandler
    {
        private readonly IService service;

        public OutputWixCommandHandler(IService service)
        {
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputWixOptions options)
        {
            var value = service.Call();
            Console.WriteLine($"Command : 'output wix', LogFile : '{options.LogFile}', WorkDir : '{options.OutputDir}', InputFile: '{options.InputFile}', WixFile: '{options.WixFile}' ");
            return Task.FromResult(value);
        }
    }
}