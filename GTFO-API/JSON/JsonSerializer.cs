using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using GTFO.API.JSON.Settings;
using NativeSerializer = System.Text.Json.JsonSerializer;

namespace GTFO.API.JSON
{
    public static class JsonSerializer
    {
        public static String Serialize(object value, JsonSerializerOptions options = null)
        {
            return NativeSerializer.Serialize(value, options);
        }

        public static T Deserialize<T>(string json, JsonSerializerOptions options = null) 
        {
            return NativeSerializer.Deserialize<T>(json, options);
        }
    }
}
