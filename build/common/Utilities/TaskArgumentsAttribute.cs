namespace Common.Utilities;

/// <summary>
/// Represents a task additional arguments.
/// </summary>
/// <seealso cref="Attribute" />
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class TaskArgumentAttribute : Attribute
{
    public string Name { get; }
    public string[] PossibleValues { get; }
    public TaskArgumentAttribute(string name, params string[] possibleValues)
    {
        Name = name;
        PossibleValues = possibleValues;
    }
}
