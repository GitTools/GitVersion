using GitVersion;

using var serviceProvider = ModulesLoader.Load();
var app = serviceProvider.GetRequiredService<GitVersionApp>();
var result = await app.RunAsync(args);

if (!Console.IsInputRedirected) Console.ReadKey();

return result;