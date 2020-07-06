using System;
using System.Threading.Tasks;
using GitVersion.Core;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Normalize
{
    public class NormalizeCommandHandler : CommandHandler<NormalizeOptions>, IRootCommandHandler
    {
        private readonly IService service;

        public NormalizeCommandHandler(IService service)
        {
            this.service = service;
        }

        public override Task<int> InvokeAsync(NormalizeOptions options)
        {
            var value = service.Call();
            Console.WriteLine($"Command : 'normalize', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}