using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GOCDApiClient.Converters;

public class StringToBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase);
        }
        
        if (reader.TokenType == JsonTokenType.True)
            return true;
            
        if (reader.TokenType == JsonTokenType.False)
            return false;
            
        return false;
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}