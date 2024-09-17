#if !NET7_0_OR_GREATER

// The namespace is important
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Fake version of the StringSyntaxAttribute, which was introduced in .NET 7</summary>
[SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
[AttributeUsage(AttributeTargets.All)]
public sealed class StringSyntaxAttribute : Attribute
{
    /// <summary>The syntax identifier for strings containing composite formats.</summary>
    public const string CompositeFormat = nameof(CompositeFormat);

    /// <summary>The syntax identifier for strings containing regular expressions.</summary>
    public const string Regex = nameof(Regex);

    /// <summary>The syntax identifier for strings containing date information.</summary>
    public const string DateTimeFormat = nameof(DateTimeFormat);

    /// <summary>
    /// Initializes a new instance of the <see cref="StringSyntaxAttribute"/> class.
    /// </summary>
    public StringSyntaxAttribute(string syntax)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringSyntaxAttribute"/> class.
    /// </summary>
    public StringSyntaxAttribute(string syntax, params object?[] arguments)
    {
    }
}
#endif
