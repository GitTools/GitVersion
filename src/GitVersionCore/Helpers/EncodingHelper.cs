using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitVersion.Helpers
{
    public static class EncodingHelper
    {
        private static IList<Encoding> encodingsWithPreambles;

        private static int maxPreambleLength;

        /// <summary>
        /// Detects the encoding of a file if and only if it includes a preamble .
        /// </summary>
        /// <param name="filename">The file name to check the encoding of.</param>
        /// <returns>The encoding of the file if it has a preamble otherwise null.</returns>
        public static Encoding DetectEncoding(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }

            if (encodingsWithPreambles == null)
            {
                ScanEncodings();
            }

            using var stream = File.OpenRead(filename);
            // No bytes? No encoding!
            if (stream.Length == 0)
            {
                return null;
            }

            // Read the minimum amount necessary.
            var length = stream.Length > maxPreambleLength ? maxPreambleLength : stream.Length;

            var bytes = new byte[length];
            stream.Read(bytes, 0, (int)length);
            return DetectEncoding(bytes);
        }

        /// <summary>
        /// Returns the first encoding where all the preamble bytes match exactly.
        /// </summary>
        /// <param name="bytes">The bytes to check for a matching preamble.</param>
        /// <returns>The encoding that has a matching preamble or null if one was not found.</returns>
        public static Encoding DetectEncoding(IList<byte> bytes)
        {
            if (bytes == null || bytes.Count == 0)
            {
                return null;
            }

            if (encodingsWithPreambles == null)
            {
                ScanEncodings();
            }

            return encodingsWithPreambles.FirstOrDefault(encoding => PreambleMatches(encoding, bytes));
        }

        /// <summary>
        /// Returns an ordered list of encodings that have preambles ordered by the length of the
        /// preamble longest to shortest. This prevents a short preamble masking a longer one
        /// later in the list.
        /// </summary>
        /// <returns>An ordered list of encodings and corresponding preambles.</returns>
        private static void ScanEncodings()
        {
            var encodings = (Encoding.GetEncodings());
            encodingsWithPreambles = (from info in encodings
                                      let encoding = info.GetEncoding()
                                      let preamble = encoding.GetPreamble()
                                      where preamble.Length > 0
                                      orderby preamble.Length descending
                                      select encoding).ToList();

            var encodingWithLongestPreamble = encodingsWithPreambles.FirstOrDefault();
            maxPreambleLength = encodingWithLongestPreamble?.GetPreamble().Length ?? 0;
        }

        /// <summary>
        /// Verifies that all bytes of an encoding's preamble are present at the beginning of some sample data.
        /// </summary>
        /// <param name="encoding">The encoding to check against.</param>
        /// <param name="data">The data to test.</param>
        /// <returns>A boolean indicating if a preamble match was found.</returns>
        private static bool PreambleMatches(Encoding encoding, IList<byte> data)
        {
            var preamble = encoding.GetPreamble();
            if (preamble.Length > data.Count)
                return false;

            return !preamble.Where((preambleByte, index) => data[index] != preambleByte).Any();
        }
    }
}
