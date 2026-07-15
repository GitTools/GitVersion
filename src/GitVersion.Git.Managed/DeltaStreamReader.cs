// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Reads delta instructions from a <see cref="Stream"/>.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format#_deltified_representation"/>
internal static class DeltaStreamReader
{
    /// <summary>
    /// Reads the next instruction from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The stream from which to read the instruction.</param>
    /// <returns>The next instruction if found; otherwise, <see langword="null"/>.</returns>
    public static DeltaInstruction? Read(Stream stream)
    {
        var next = stream.ReadByte();

        if (next == -1)
        {
            return null;
        }

        var instruction = (byte)next;

        DeltaInstruction value;
        value.Offset = 0;
        value.Size = 0;

        value.InstructionType = (DeltaInstructionType)((instruction & 0b1000_0000) >> 7);

        if (value.InstructionType == DeltaInstructionType.Insert)
        {
            value.Size = instruction & 0b0111_1111;
        }
        else
        {
            ReadCopyInstruction(stream, instruction, ref value);
        }

        return value;
    }

    private static void ReadCopyInstruction(Stream stream, byte instruction, ref DeltaInstruction value)
    {
        if ((instruction & 0b0000_0001) != 0)
        {
            value.Offset |= ReadByteOrThrow(stream);
        }

        if ((instruction & 0b0000_0010) != 0)
        {
            value.Offset |= ReadByteOrThrow(stream) << 8;
        }

        if ((instruction & 0b0000_0100) != 0)
        {
            value.Offset |= ReadByteOrThrow(stream) << 16;
        }

        if ((instruction & 0b0000_1000) != 0)
        {
            value.Offset |= ReadByteOrThrow(stream) << 24;
        }

        if ((instruction & 0b0001_0000) != 0)
        {
            value.Size = ReadByteOrThrow(stream);
        }

        if ((instruction & 0b0010_0000) != 0)
        {
            value.Size |= ReadByteOrThrow(stream) << 8;
        }

        if ((instruction & 0b0100_0000) != 0)
        {
            value.Size |= ReadByteOrThrow(stream) << 16;
        }

        // Size zero is automatically converted to 0x10000.
        if (value.Size == 0)
        {
            value.Size = 0x10000;
        }
    }

    private static int ReadByteOrThrow(Stream stream)
    {
        var value = stream.ReadByte();
        return value == -1
            ? throw new EndOfStreamException("The delta stream ended in the middle of a copy instruction.")
            : value;
    }
}
