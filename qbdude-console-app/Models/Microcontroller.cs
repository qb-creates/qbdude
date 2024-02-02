using Newtonsoft.Json;
using QBdude.Converters;

namespace QBdude.Models;

public sealed class Microcontroller
{
    private Microcontroller() { }

    /// <summary>
    /// The microcontroller's name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The microcontrollers signature.
    /// </summary>
    [JsonProperty("signature")]
    [JsonConverter(typeof(HexByteArrayConverter))]
    public IEnumerable<byte> Signature { get; private set; }

    /// <summary>
    /// Total available flash of the microcontroller.
    /// </summary>
    [JsonProperty("flashSize")]
    public int FlashSize { get; private set; }

    /// <summary>
    /// The page size of the microcontroller.
    /// </summary>
    [JsonProperty("pageSize")]
    public int PageSize { get; private set; }

    /// <summary>
    /// Dictionary that contains the different boot size options this microcontroller has.
    /// </summary>
    [JsonProperty("bootFlashSizeDictionary")]
    public IReadOnlyDictionary<string, int> BootFlashSizeDictionary { get; private set; } = new Dictionary<string, int>();

    /// <summary>
    /// Byte mask that will be compared against the microcontroller's high fuse bits to see if the boot reset vector is eneabled. 
    /// </summary>
    [JsonProperty("bootConfigMask")]
    [JsonConverter(typeof(HexByteConverter))]
    public byte BootConfigMask { get; private set; }

    /// <summary>
    /// Get the microcontroller's boot section flash size.
    /// </summary>
    /// <param name="highFuseBits">The microcontroller's configured high fuse bits.</param>
    /// <param name="bootResetEnabled">Is the boot reset bit enabled for the microcontroller</param>
    /// <returns>Returns the configured boot section flash size. Returns 0 if the Boot Reset Vector isn't enabled.</returns>
    public int GetBootConfigSize(byte highFuseBits, out bool bootResetEnabled)
    {
        string bootConfigByte = $"0x{highFuseBits & BootConfigMask:X2}";

        if (!BootFlashSizeDictionary.ContainsKey(bootConfigByte))
        {
            bootResetEnabled = false;
            return 0;
        }

        bootResetEnabled = true;
        return BootFlashSizeDictionary[bootConfigByte];
    }
}