using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BarPlugin.InteractionGame.Utils
{

    public class IntegerOnlyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // This converter is intended for numeric types.
            return objectType == typeof(double) || objectType == typeof(float) || objectType == typeof(decimal);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Read the value as a double.
            double value = (double)JToken.Load(reader);

            // Return the integer part by truncating the decimal.
            return (int)value;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Write the value as is (not used in this context).
            writer.WriteValue(value);
        }
    }
}
