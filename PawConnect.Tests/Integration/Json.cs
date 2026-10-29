using System.Text.Json;
using System.Text.Json.Serialization;

namespace PawConnect.Tests.Integration;

public static class Json
{
    // The API sends camelCase JSON and writes enums as strings.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}