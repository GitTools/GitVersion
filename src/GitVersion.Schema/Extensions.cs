using System.Text.Encodings.Web;
using Json.More;
using Json.Schema;

namespace GitVersion.Schema;

internal static class Extensions
{
    extension(JsonSchema jsonSchema)
    {
        public void WriteToFile(string outputFileName)
        {
            var jsonDocument = jsonSchema.ToJsonDocument();

            using var fs = File.Create(outputFileName);
            using var writer = new Utf8JsonWriter(fs, new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            jsonDocument.WriteTo(writer);
            writer.Flush();
            fs.Flush();
        }
    }
}
