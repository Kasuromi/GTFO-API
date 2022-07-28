using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO.API.Utilities;
using UnityEngine;

namespace GTFO.API.JSON.Converters
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public override bool HandleNull => false;

        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Vector3 vector = new Vector3();

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    int depth = reader.CurrentDepth;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == depth)
                            return vector;

                        if (reader.TokenType != JsonTokenType.PropertyName)
                            throw new JsonException("Expected PropertyName token");

                        string propName = reader.GetString();
                        reader.Read();

                        switch (propName.ToLowerInvariant())
                        {
                            case "x":
                                vector.x = reader.GetSingle();
                                break;

                            case "y":
                                vector.y = reader.GetSingle();
                                break;

                            case "z":
                                vector.z = reader.GetSingle();
                                break;
                        }
                    }
                    throw new JsonException("Expected EndObject token");

                case JsonTokenType.String:
                    string strValue = reader.GetString().Trim();
                    if (TryParseVector3(strValue, out vector))
                    {
                        return vector;
                    }
                    throw new JsonException($"Vector3 format is not right: {strValue}");

                default:
                    throw new JsonException($"Vector3Json type: {reader.TokenType} is not implemented!");
            }
        }

        private static bool TryParseVector3(string input, out Vector3 vector)
        {
            if (!RegexUtils.TryParseVectorString(input, out float[] array))
            {
                vector = Vector3.zero;
                return false;
            }

            if (array.Length < 3)
            {
                vector = Vector3.zero;
                return false;
            }

            vector = new Vector3(array[0], array[1], array[2]);
            return true;
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(string.Format("({0} {1} {2})", value.x, value.y, value.z));
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
