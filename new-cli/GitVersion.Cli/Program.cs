using GitVersion;

var assemblies = new[]
{
    typeof(CoreModule).Assembly,
    typeof(LibGit2SharpCoreModule).Assembly,
    typeof(NormalizeModule).Assembly,
    typeof(CalculateModule).Assembly,
    typeof(ConfigModule).Assembly,
    typeof(OutputModule).Assembly,
    typeof(CliModule).Assembly
};

using var serviceProvider = ModulesLoader.Load(assemblies);
var app = serviceProvider.GetRequiredService<GitVersionApp>();
var result = await app.RunAsync(args);

if (!Console.IsInputRedirected) Console.ReadKey();

return result;
