using System;
using System.Threading.Tasks;
using Core;

namespace Output
{
    public class OutputAssemblyInfoCommandHandler : CommandHandler<OutputAssemblyInfoOptions>, IOutputCommandHandler
    {
        private readonly IService service;

        public OutputAssemblyInfoCommandHandler(IService service)
        {
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputAssemblyInfoOptions options)
        {
            var value = service.Call();
            var versionInfo = options.VersionInfo.Value;
            Console.WriteLine($"Command : 'output assemblyinfo', LogFile : '{options.LogFile}', WorkDir : '{options.OutputDir}', InputFile: '{options.InputFile}', AssemblyInfo: '{options.AssemblyinfoFile}' ");
            Console.WriteLine($"Version info: {versionInfo}");
            return Task.FromResult(value);
        }
    }
}