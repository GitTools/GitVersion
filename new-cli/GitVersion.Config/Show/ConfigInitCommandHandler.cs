using System;
using System.Threading.Tasks;
using GitVersion.Core;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Config.Show
{
    public class ConfigShowCommandHandler : CommandHandler<ConfigShowOptions>, IConfigCommandHandler
    {
        private readonly IService service;

        public ConfigShowCommandHandler(IService service)
        {
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigShowOptions options)
        {
            var value = service.Call();
            Console.WriteLine($"Command : 'config show', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}