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
