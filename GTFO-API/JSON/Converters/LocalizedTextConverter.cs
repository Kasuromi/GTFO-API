using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Localization;

namespace GTFO.API.JSON.Converters
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public sealed class LocalizedTextConverter : JsonConverter<LocalizedText>
    {
        public override bool HandleNull => false;

        public override LocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var strValue = reader.GetString();
                    return new LocalizedText
                    {
                        Id = 0,
                        UntranslatedText = strValue
                    };


                case JsonTokenType.Number:
                    return new LocalizedText()
                    {
                        Id = reader.GetUInt32(),
                        UntranslatedText = null
                    };

                default:
                    throw new JsonException($"LocalizedTextJson type: {reader.TokenType} is not implemented!");
            }
        }

        public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options)
        {
            if (value.Id != 0u) writer.WriteNumberValue(0u);
            else if (value.HasTranslation) writer.WriteNumberValue(0u);
            else writer.WriteNullValue();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
