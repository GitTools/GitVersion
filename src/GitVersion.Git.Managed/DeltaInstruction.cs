// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Enumerates the instruction types which can be found in a deltified stream.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format#_deltified_representation"/>
internal enum DeltaInstructionType
{
    /// <summary>
    /// Instructs the caller to insert a new byte range into the object.
    /// </summary>
    Insert = 0,

    /// <summary>
    /// Instructs the caller to copy a byte range from the source object.
    /// </summary>
    Copy = 1
}

/// <summary>
/// Represents an instruction in a deltified stream.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format#_deltified_representation"/>
internal struct DeltaInstruction
{
    /// <summary>
    /// The type of the current instruction.
    /// </summary>
    public DeltaInstructionType InstructionType;

    /// <summary>
    /// If the <see cref="InstructionType"/> is <see cref="DeltaInstructionType.Copy"/>,
    /// the offset of the base stream to start copying from.
    /// </summary>
    public int Offset;

    /// <summary>
    /// The number of bytes to copy or insert.
    /// </summary>
    public int Size;
}
