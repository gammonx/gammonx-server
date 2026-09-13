using System.Text.Json;
using System.Text.Json.Serialization;

namespace GammonX.Models.Helpers
{
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("A UTC timestamp must be a JSON string.");

            try
            {
                return DateTimeHelper.ParseUtc(reader.GetString()!);
            }
            catch (FormatException exception)
            {
                throw new JsonException("The timestamp is not a valid UTC timestamp.", exception);
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(DateTimeHelper.FormatUtc(value));
        }
    }
}