using System.IO;

namespace GitVersionCore.Tests
{
    public class TestStream : Stream
    {
        private readonly string path;
        private readonly TestFileSystem testFileSystem;
        private readonly MemoryStream underlying = new MemoryStream();

        public TestStream(string path, TestFileSystem testFileSystem)
        {
            this.path = path;
            this.testFileSystem = testFileSystem;
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            underlying.Position = 0;
            var readToEnd = new StreamReader(underlying).ReadToEnd();
            testFileSystem.WriteAllText(path, readToEnd);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return underlying.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            underlying.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return underlying.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            underlying.Write(buffer, offset, count);
        }

        public override bool CanRead => underlying.CanRead;
        public override bool CanSeek => underlying.CanSeek;
        public override bool CanWrite => underlying.CanWrite;
        public override long Length => underlying.Length;

        public override long Position
        {
            get => underlying.Position;
            set => underlying.Position = value;
        }
    }
}
