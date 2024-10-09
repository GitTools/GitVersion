namespace Common.Utilities;

/// <summary>
/// Represents a task additional arguments.
/// </summary>
/// <seealso cref="Attribute" />
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class TaskArgumentAttribute(string name, params string[] possibleValues) : Attribute
{
    public string Name { get; } = name;
    public string[] PossibleValues { get; } = possibleValues;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DotnetArgumentAttribute()
    : TaskArgumentAttribute(Arguments.DotnetTarget, Constants.Frameworks);

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DockerDotnetArgumentAttribute()
    : TaskArgumentAttribute(Arguments.DockerDotnetVersion, Constants.Frameworks);

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DockerDistroArgumentAttribute()
    : TaskArgumentAttribute(Arguments.DockerDistro, Constants.DockerDistrosToBuild);

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DockerRegistryArgumentAttribute()
    : TaskArgumentAttribute(Arguments.DockerRegistry, Constants.DockerRegistries);

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ArchitectureArgumentAttribute()
    : TaskArgumentAttribute(Arguments.Architecture, Constants.Architectures);
