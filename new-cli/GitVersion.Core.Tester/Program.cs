using GitVersion;

var assemblies = new[]
{
    typeof(CoreModule).Assembly,
    typeof(TesterModule).Assembly,
};

using var serviceProvider = ModulesLoader.Load(assemblies);
var app = serviceProvider.GetRequiredService<GitVersionApp>();
var result = await app.RunAsync(args);

if (!Console.IsInputRedirected) Console.ReadKey();

return result;
