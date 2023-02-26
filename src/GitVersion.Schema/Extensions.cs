using Json.More;
using Json.Schema;

internal static class Extensions
{
    public static void WriteToFile(this JsonSchema jsonSchema, string outputFileName)
    {
        var jsonDocument = jsonSchema.ToJsonDocument();

        using FileStream fs = File.Create(outputFileName);
        using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        jsonDocument.WriteTo(writer);
        writer.Flush();
        fs.Flush();
    }
}
