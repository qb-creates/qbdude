using Newtonsoft.Json;
using Serilog;

namespace qbdude.converters;

/// <summary>
/// Converts a byte to and from a string representation of a hex literal. 
/// Example: "0xAA" is converted to 0xAA and vice versa.
/// </summary>
public class HexByteConverter : JsonConverter<byte>
{
    public override byte ReadJson(JsonReader reader, Type objectType, byte existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        try
        {
            if (reader.TokenType == JsonToken.String)
            {
                string hexString = reader.Value.ToString();

                if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    hexString = hexString.Substring(2);
                }

                return byte.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        return 0x00;
    }

    public override void WriteJson(JsonWriter writer, byte value, JsonSerializer serializer)
    {
        writer.WriteValue($"0x{value:X2}");
    }
}