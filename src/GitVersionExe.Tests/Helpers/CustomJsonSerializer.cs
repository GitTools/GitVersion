using System.Collections.Generic;
using System.Text.Json;

namespace GitVersionExe.Tests.Helpers
{
    public class CustomJsonSerializer
    {
        public static T Deserialize<T>(string json)
        {
            var data = JsonSerializer.Deserialize<T>(json);

            if (data is Dictionary<string, object> odata)
            {
                var ndata = new Dictionary<string, object>();
                foreach (var key in odata.Keys)
                {
                    var value = (JsonElement)odata[key];
                    switch (value.ValueKind)
                    {
                        case JsonValueKind.Undefined:
                            break;
                        case JsonValueKind.Object:
                            break;
                        case JsonValueKind.Array:
                            break;
                        case JsonValueKind.String:
                            ndata.Add(key, value.GetString());
                            break;
                        case JsonValueKind.Number:
                            ndata.Add(key, value.GetInt64().ToString());
                            break;
                        case JsonValueKind.True:
                            break;
                        case JsonValueKind.False:
                            break;
                        case JsonValueKind.Null:
                            break;
                    }
                }

                if (ndata is T obj) return obj;
            }

            return data;
        }
    }
}
