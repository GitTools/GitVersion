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
            if ((instruction & 0b0000_0001) != 0)
            {
                value.Offset |= (byte)stream.ReadByte();
            }

            if ((instruction & 0b0000_0010) != 0)
            {
                value.Offset |= (byte)stream.ReadByte() << 8;
            }

            if ((instruction & 0b0000_0100) != 0)
            {
                value.Offset |= (byte)stream.ReadByte() << 16;
            }

            if ((instruction & 0b0000_1000) != 0)
            {
                value.Offset |= (byte)stream.ReadByte() << 24;
            }

            if ((instruction & 0b0001_0000) != 0)
            {
                value.Size = (byte)stream.ReadByte();
            }

            if ((instruction & 0b0010_0000) != 0)
            {
                value.Size |= (byte)stream.ReadByte() << 8;
            }

            if ((instruction & 0b0100_0000) != 0)
            {
                value.Size |= (byte)stream.ReadByte() << 16;
            }

            // Size zero is automatically converted to 0x10000.
            if (value.Size == 0)
            {
                value.Size = 0x10000;
            }
        }

        return value;
    }
}
