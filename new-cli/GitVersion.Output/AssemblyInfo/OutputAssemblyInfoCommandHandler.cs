using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.AssemblyInfo
{
    public class OutputAssemblyInfoCommandHandler : CommandHandler<OutputAssemblyInfoOptions>, IOutputCommandHandler
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputAssemblyInfoCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputAssemblyInfoOptions options)
        {
            var value = service.Call();
            var versionInfo = options.VersionInfo.Value;
            logger.LogInformation($"Command : 'output assemblyinfo', LogFile : '{options.LogFile}', WorkDir : '{options.OutputDir}', InputFile: '{options.InputFile}', AssemblyInfo: '{options.AssemblyinfoFile}' ");
            logger.LogInformation($"Version info: {versionInfo}");
            return Task.FromResult(value);
        }
    }
}