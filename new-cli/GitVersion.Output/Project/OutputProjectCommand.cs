using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.Project
{
    public class OutputProjectCommand : Command<OutputProjectSettings>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputProjectCommand(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputProjectSettings settings)
        {
            var value = service.Call();
            logger.LogInformation(
                $"Command : 'output project', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', Project: '{settings.ProjectFile}' ");
            return Task.FromResult(value);
        }
    }
}