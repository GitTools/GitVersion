using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion;
using Microsoft.Build.Framework;
using System.IO;
using GitTools;

public abstract class AssemblyInfoBuilder
{
    private static readonly Dictionary<string, Type> assemblyInfoBuilders = new Dictionary<string, Type>()
    {
        { ".cs", typeof(CSharpAssemblyInfoBuilder) },
        { ".vb", typeof(VisualBasicAssemblyInfoBuilder) }
        // TODO: Missing FSharpAssemblyInfoBuilder
    };

    public static AssemblyInfoBuilder GetAssemblyInfoBuilder(IEnumerable<ITaskItem> compileFiles)
    {
        Type builderType;

        var assemblyInfoExtension = compileFiles.Select(x => x.ItemSpec)
            .Where(compileFile => compileFile.Contains("AssemblyInfo"))
            .Select(Path.GetExtension).FirstOrDefault();

        if (assemblyInfoBuilders.TryGetValue(assemblyInfoExtension, out builderType))
        {
            return Activator.CreateInstance(builderType) as AssemblyInfoBuilder;
        }

        throw new WarningException("Unable to determine which AssemblyBuilder required to generate GitVersion assembly information");
    }

    public abstract string AssemblyInfoExtension { get; }

    public abstract string GetAssemblyInfoText(VersionVariables vars, string rootNamespace);
}
