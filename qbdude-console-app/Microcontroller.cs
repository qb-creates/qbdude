namespace qbdude.Models;

/// <summary>
/// This class holds a dictionary of all supported microcontrollers. Their name, signature, flash size, and page size can be retrieved.
/// </summary>
public sealed class Microcontroller
{
    public static Dictionary<string, Microcontroller> DeviceDictionary = new Dictionary<string, Microcontroller>() 
    {
        {
            "m128", new Microcontroller()
                    { 
                        Name = "ATmega128", 
                        Signature = new byte[] {0x1E, 0x97, 0x02}, 
                        FlashSize = 131072, 
                        PageSize = 256, 
                        BootConfigMask = 0x07,
                        BootFlashSizeDictionary = new Dictionary<int, int>() 
                        {
                            {0x00, 8192},
                            {0x02, 4086},
                            {0x04, 2048},
                            {0x06, 1024}
                        }
                    }

        },
        {
            "m1284", new Microcontroller()
                    { 
                        Name = "ATmega1284", 
                        Signature = new byte[] {0x1E, 0x97, 0x06}, 
                        FlashSize = 131072, 
                        PageSize = 256, 
                        BootConfigMask = 0x07,
                        BootFlashSizeDictionary = new Dictionary<int, int>() 
                        {
                            {0x00, 8192},
                            {0x02, 4086},
                            {0x04, 2048},
                            {0x06, 1024}
                        }
                    }
        },
        {
            "m32", new Microcontroller(){ Name = "ATmega32", Signature = new byte[] {0x1E, 0x95, 0x02}, FlashSize = 32768, PageSize = 128}
        }
    };

    private Microcontroller() { }

    /// <summary>
    /// The microcontroller's name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The microcontrollers signature.
    /// </summary>
    public IEnumerable<byte> Signature { get; private set; } = new byte[0];

    /// <summary>
    /// Total available flash of the microcontroller.
    /// </summary>
    public int FlashSize { get; private set; }

    /// <summary>
    /// The page size of the microcontroller.
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public byte BootConfigMask { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyDictionary<int, int> BootFlashSizeDictionary { get; private set; } = new Dictionary<int, int>();
}
