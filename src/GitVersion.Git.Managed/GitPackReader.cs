// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

internal static class GitPackReader
{
    /// <summary>
    /// Reads a Git object which is stored at a given offset in a Git pack file.
    /// </summary>
    /// <param name="pack">The pack from which to read the object.</param>
    /// <param name="stream">A (seekable) stream over the pack file. Ownership is transferred to the returned stream.</param>
    /// <param name="offset">The offset of the object in the pack file.</param>
    /// <param name="objectType">The expected object type, or <see langword="null"/> to accept any type.</param>
    /// <returns>A stream over the object data, and the actual object type.</returns>
    public static (Stream Stream, string ObjectType) GetObject(GitPack pack, Stream stream, long offset, string? objectType)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            stream.Seek(offset, SeekOrigin.Begin);

            var (type, decompressedSize) = ReadObjectHeader(stream);

            switch (type)
            {
                case GitPackObjectType.OfsDelta:
                    {
                        var baseObjectRelativeOffset = ReadVariableLengthInteger(stream);
                        var baseObjectOffset = offset - baseObjectRelativeOffset;

                        var deltaStream = new GitZLibStream(stream, decompressedSize);
                        var (baseStream, baseType) = pack.GetObject(baseObjectOffset, objectType);

                        return (new GitPackDeltafiedStream(baseStream, deltaStream), baseType);
                    }

                case GitPackObjectType.RefDelta:
                    {
                        Span<byte> baseObjectId = stackalloc byte[GitObjectId.Sha1Size];
                        stream.ReadExactly(baseObjectId);

                        var baseObject = pack.ResolveBaseObject(GitObjectId.Parse(baseObjectId), objectType)
                            ?? throw new GitObjectStoreException($"The base object of the deltified object at offset {offset} could not be found.") { ObjectNotFound = true };

                        var seekableBaseObject = new GitPackMemoryCacheStream(baseObject.Stream);
                        var deltaStream = new GitZLibStream(stream, decompressedSize);

                        return (new GitPackDeltafiedStream(seekableBaseObject, deltaStream), baseObject.ObjectType);
                    }

                default:
                    {
                        var actualType = type switch
                        {
                            GitPackObjectType.Commit => GitObjectTypes.Commit,
                            GitPackObjectType.Tree => GitObjectTypes.Tree,
                            GitPackObjectType.Blob => GitObjectTypes.Blob,
                            GitPackObjectType.Tag => GitObjectTypes.Tag,
                            _ => throw new GitObjectStoreException($"The object at offset {offset} has an unsupported pack object type '{type}'.")
                        };

                        if (objectType is not null && actualType != objectType)
                        {
                            throw new GitObjectStoreException($"An object of type {objectType} could not be located at offset {offset}.") { ObjectNotFound = true };
                        }

                        return (new GitZLibStream(stream, decompressedSize), actualType);
                    }
            }
        }
        catch (EndOfStreamException eof)
        {
            throw new GitObjectStoreException($"An object could not be located at offset {offset}.", eof) { ObjectNotFound = true };
        }
    }

    private static (GitPackObjectType ObjectType, long Length) ReadObjectHeader(Stream stream)
    {
        Span<byte> value = stackalloc byte[1];
        stream.ReadExactly(value);

        var type = (GitPackObjectType)((value[0] & 0b0111_0000) >> 4);
        long length = value[0] & 0b_1111;

        if ((value[0] & 0b1000_0000) == 0)
        {
            return (type, length);
        }

        var shift = 4;

        do
        {
            stream.ReadExactly(value);
            length |= (value[0] & 0b0111_1111L) << shift;
            shift += 7;
        }
        while ((value[0] & 0b1000_0000) != 0);

        return (type, length);
    }

    private static long ReadVariableLengthInteger(Stream stream)
    {
        long offset = -1;
        int b;

        do
        {
            offset++;
            b = stream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }

            offset = (offset << 7) + (b & 127);
        }
        while ((b & 128) != 0);

        return offset;
    }
}
