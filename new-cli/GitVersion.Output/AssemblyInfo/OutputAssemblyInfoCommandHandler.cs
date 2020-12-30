using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.AssemblyInfo
{
    public class OutputAssemblyInfoCommandHandler : CommandHandler<OutputAssemblyInfoCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputAssemblyInfoCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputAssemblyInfoCommand command)
        {
            var value = service.Call();
            var versionInfo = command.VersionInfo.Value;
            logger.LogInformation(
                $"Command : 'output assemblyinfo', LogFile : '{command.LogFile}', WorkDir : '{command.OutputDir}', InputFile: '{command.InputFile}', AssemblyInfo: '{command.AssemblyinfoFile}' ");
            logger.LogInformation($"Version info: {versionInfo}");
            return Task.FromResult(value);
        }
    }
}