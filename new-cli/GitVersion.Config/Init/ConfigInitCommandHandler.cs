using System;
using System.Threading.Tasks;
using GitVersion.Core;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Config.Init
{
    public class ConfigInitCommandHandler : CommandHandler<ConfigInitOptions>, IConfigCommandHandler
    {
        private readonly IService service;

        public ConfigInitCommandHandler(IService service)
        {
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigInitOptions options)
        {
            var value = service.Call();
            Console.WriteLine($"Command : 'config init', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}