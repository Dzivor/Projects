using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankStatementAPI.Helpers
{
    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString() ?? "";

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetDecimal(out decimal d))
                    return d.ToString(
                        System.Globalization.CultureInfo
                            .InvariantCulture);

                return reader.GetDouble().ToString(
                    System.Globalization.CultureInfo
                        .InvariantCulture);
            }

            if (reader.TokenType == JsonTokenType.Null)
                return "";

            reader.Skip();
            return "";
        }

        public override void Write(
            Utf8JsonWriter writer,
            string value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}