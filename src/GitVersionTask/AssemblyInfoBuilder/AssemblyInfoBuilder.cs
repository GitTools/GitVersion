using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitTools;
using GitVersion;
using Microsoft.Build.Framework;

public abstract class AssemblyInfoBuilder
{
    private static readonly Dictionary<string, Type> assemblyInfoBuilders = new Dictionary<string, Type>
    {
        {".cs", typeof(CSharpAssemblyInfoBuilder)},
        {".vb", typeof(VisualBasicAssemblyInfoBuilder)}
        // TODO: Missing FSharpAssemblyInfoBuilder
    };

    public abstract string AssemblyInfoExtension { get; }

    public static AssemblyInfoBuilder GetAssemblyInfoBuilder(IEnumerable<ITaskItem> compileFiles)
    {
        if (compileFiles == null)
        {
            throw new ArgumentNullException("compileFiles");
        }

        Type builderType;

        var assemblyInfoExtension = compileFiles
            .Select(x => x.ItemSpec)
            .Select(Path.GetExtension)
            // TODO: While it works, this seems like a bad way to discover the language is being compiled. @asbjornu
            .FirstOrDefault(extension => assemblyInfoBuilders.ContainsKey(extension.ToLowerInvariant()));

        if (assemblyInfoExtension != null && assemblyInfoBuilders.TryGetValue(assemblyInfoExtension, out builderType))
        {
            return Activator.CreateInstance(builderType) as AssemblyInfoBuilder;
        }

        throw new WarningException("Unable to determine which AssemblyBuilder required to generate GitVersion assembly information");
    }

    public abstract string GetAssemblyInfoText(VersionVariables vars, string rootNamespace);
}