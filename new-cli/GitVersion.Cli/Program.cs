using GitVersion;

using var serviceProvider = ModulesLoader.Load();
var app = serviceProvider.GetService<GitVersionApp>();
var result = await app!.RunAsync(args);

if (!Console.IsInputRedirected) Console.ReadKey();

return result;