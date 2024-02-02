using Newtonsoft.Json;
using Serilog;

namespace qbdude.converters;

/// <summary>
/// Converts an array of bytes to and from an array of hex literal as strings. 
/// Example: ["0xAA", "0x1E"] is converted to [0xAA, 0x1E] and vice versa.
/// </summary>
public class HexByteArrayConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(byte[]);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                List<string> hexStrings = serializer.Deserialize<List<string>>(reader);

                if (hexStrings != null)
                {
                    byte[] byteArray = new byte[hexStrings.Count];

                    for (int i = 0; i < hexStrings.Count; i++)
                    {
                        string hexString = hexStrings[i];

                        if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            hexString = hexString.Substring(2);
                        }

                        byteArray[i] = byte.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                    }

                    return byteArray;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        return new byte[] { 0, 0, 0 };
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value.GetType() == typeof(byte[]))
        {
            byte[] byteArray = (byte[])value;
            List<string> hexStrings = new List<string>();

            for (int i = 0; i < byteArray.Length; i++)
            {
                hexStrings.Add($"0x{byteArray[i]:X2}");
            }

            serializer.Serialize(writer, hexStrings);
        }
    }
}