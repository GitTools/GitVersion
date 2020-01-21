using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GitVersion.OutputVariables;

namespace GitVersion.Helpers
{
    // Credit to https://github.com/neuecc
    // Inspired by https://gist.github.com/neuecc/7d728cd99d2a1e613362
    public static class JsonSerializer
    {
        private const string INDENT_STRING = "  ";
        private static readonly Encoding UTF8 = new UTF8Encoding(false);

        public static string Serialize(object obj)
        {
            using var ms = new MemoryStream();
            using var sw = new StreamWriter(ms, UTF8);
            Serialize(sw, obj);
            sw.Flush();
            return UTF8.GetString(ms.ToArray());
        }

        public static void Serialize(TextWriter tw, object obj)
        {
            SerializeObject(tw, obj);
        }

        enum JsonType
        {
            @string,
            number,
            boolean,
            @object,
            array,
            @null
        }

        static JsonType GetJsonType(object obj)
        {
            if (obj == null) return JsonType.@null;

            return Type.GetTypeCode(obj.GetType()) switch
            {
                TypeCode.Boolean => JsonType.boolean,
                TypeCode.String => JsonType.@string,
                TypeCode.Char => JsonType.@string,
                TypeCode.DateTime => JsonType.@string,
                TypeCode.Int16 => JsonType.number,
                TypeCode.Int32 => JsonType.number,
                TypeCode.Int64 => JsonType.number,
                TypeCode.UInt16 => JsonType.number,
                TypeCode.UInt32 => JsonType.number,
                TypeCode.UInt64 => JsonType.number,
                TypeCode.Single => JsonType.number,
                TypeCode.Double => JsonType.number,
                TypeCode.Decimal => JsonType.number,
                TypeCode.SByte => JsonType.number,
                TypeCode.Byte => JsonType.number,
                TypeCode.Object => (obj switch
                {
                    VersionVariables _ => JsonType.@object,
                    // specialized for well known types
                    Uri _ => JsonType.@string,
                    DateTimeOffset _ => JsonType.@string,
                    Guid _ => JsonType.@string,
                    StringBuilder _ => JsonType.@string,
                    IDictionary _ => JsonType.@object,
                    _ => ((obj is IEnumerable) ? JsonType.array : JsonType.@object)
                }),
                TypeCode.DBNull => JsonType.@null,
                TypeCode.Empty => JsonType.@null,
                _ => JsonType.@null
            };
        }

        static void SerializeObject(TextWriter tw, object o)
        {
            switch (GetJsonType(o))
            {
                case JsonType.@string:
                    switch (o)
                    {
                        case string s when NotAPaddedNumber(s) && int.TryParse(s, out var n):
                            WriteNumber(tw, n);
                            break;
                        case string s:
                            WriteString(tw, s);
                            break;
                        case DateTime time:
                        {
                            var s = time.ToString("o");
                            WriteString(tw, s);
                            break;
                        }
                        case DateTimeOffset offset:
                        {
                            var s = offset.ToString("o");
                            WriteString(tw, s);
                            break;
                        }
                        default:
                            WriteString(tw, o.ToString());
                            break;
                    }

                    break;
                case JsonType.number:
                    WriteNumber(tw, o);
                    break;
                case JsonType.boolean:
                    WriteBoolean(tw, (bool) o);
                    break;
                case JsonType.@object:
                    WriteObject(tw, o);
                    break;
                case JsonType.array:
                    WriteArray(tw, (IEnumerable) o);
                    break;
                case JsonType.@null:
                    WriteNull(tw);
                    break;
                default:
                    break;
            }
        }

        static void WriteString(TextWriter tw, string o)
        {
            tw.Write('\"');

            foreach (var c in o)
            {
                switch (c)
                {
                    case '"':
                        tw.Write("\\\"");
                        break;
                    case '\\':
                        tw.Write("\\\\");
                        break;
                    case '\b':
                        tw.Write("\\b");
                        break;
                    case '\f':
                        tw.Write("\\f");
                        break;
                    case '\n':
                        tw.Write("\\n");
                        break;
                    case '\r':
                        tw.Write("\\r");
                        break;
                    case '\t':
                        tw.Write("\\t");
                        break;
                    default:
                        tw.Write(c);
                        break;
                }
            }

            tw.Write('\"');
        }

        static void WriteNumber(TextWriter tw, object o)
        {
            tw.Write(o.ToString());
        }

        static void WriteBoolean(TextWriter tw, bool o)
        {
            tw.Write(o ? "true" : "false");
        }

        static void WriteObject(TextWriter tw, object o)
        {
            tw.Write('{');

            if (o is IDictionary dict)
            {
                // Dictionary
                var isFirst = true;
                foreach (DictionaryEntry item in dict)
                {
                    if (!isFirst) tw.Write(",");
                    else isFirst = false;

                    tw.Write('\"');
                    tw.Write(item.Key);
                    tw.Write('\"');
                    tw.Write(":");
                    SerializeObject(tw, item.Value);
                }
            }
            else
            {
                // Object
                var isFirst = true;
                foreach (var item in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).Where(p => !p.GetCustomAttributes(typeof(VersionVariables.ReflectionIgnoreAttribute), false).Any()))
                {
                    if (!isFirst) tw.Write(",");
                    else isFirst = false;

                    var key = item.Name;
                    var value = item.GetGetMethod().Invoke(o, null); // safe reflection for unity
                    tw.Write('\"');
                    tw.Write(key);
                    tw.Write('\"');
                    tw.Write(":");
                    SerializeObject(tw, value);
                }

                isFirst = true;
                foreach (var item in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField))
                {
                    if (!isFirst) tw.Write(",");
                    else isFirst = false;

                    var key = item.Name;
                    var value = item.GetValue(o);
                    tw.Write('\"');
                    tw.Write(key);
                    tw.Write('\"');
                    tw.Write(":");
                    SerializeObject(tw, value);
                }
            }

            tw.Write('}');
        }

        static void WriteArray(TextWriter tw, IEnumerable o)
        {
            tw.Write("[");
            var isFirst = true;
            foreach (var item in o)
            {
                if (!isFirst) tw.Write(",");
                else isFirst = false;

                SerializeObject(tw, item);
            }

            tw.Write("]");
        }

        static void WriteNull(TextWriter tw)
        {
            tw.Write("null");
        }

        private static bool NotAPaddedNumber(string value) => value == "0" || !value.StartsWith("0");

        public static string FormatJson(string json)
        {
            var indentation = 0;
            var quoteCount = 0;
            var result =
                from ch in json
                let quotes = ch == '"' ? quoteCount++ : quoteCount
                let lineBreak = ch == ',' && quotes % 2 == 0 ? ch + System.Environment.NewLine + string.Concat(Enumerable.Repeat(INDENT_STRING, indentation)) : null
                let openChar = ch == '{' || ch == '[' ? ch + System.Environment.NewLine + string.Concat(Enumerable.Repeat(INDENT_STRING, ++indentation)) : ch.ToString()
                let closeChar = ch == '}' || ch == ']' ? System.Environment.NewLine + string.Concat(Enumerable.Repeat(INDENT_STRING, --indentation)) + ch : ch.ToString()
                select lineBreak ??
                       (openChar.Length > 1
                           ? openChar
                           : closeChar);

            return string.Concat(result);
        }
    }
}
