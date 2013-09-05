using Mono.Cecil;

public static class WeavingHelper
{
    public static void WeaveAssembly(string assemblyPath)
    {
        var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath);
        var currentDirectory = AssemblyLocation.CurrentDirectory();
        var weavingTask = new PartialStubModuleWeaver
                          {
                              ModuleDefinition = moduleDefinition,
                              AddinDirectoryPath = currentDirectory,
                              SolutionDirectoryPath = currentDirectory,
                              AssemblyFilePath = assemblyPath,
                          };

        weavingTask.Execute();
        moduleDefinition.Write(assemblyPath);
        weavingTask.AfterWeaving();
    }
}