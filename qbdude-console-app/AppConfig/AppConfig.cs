using Newtonsoft.Json;
using QBdude.Models;

namespace QBdude.Config;

/// <summary>
/// Static class used to configure the application.
/// </summary>
public static class AppConfig
{
    public static IReadOnlyDictionary<string, Microcontroller> DeviceDictionary { get; private set; } = new Dictionary<string, Microcontroller>();

    public static void Configure()
    {
        GetMicrocontrollerList();
    }

    private static void GetMicrocontrollerList()
    {
        using (StreamReader file = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}\\microcontrollers.json"))
        {
            try
            {
                string json = file.ReadToEnd();
                DeviceDictionary = JsonConvert.DeserializeObject<Dictionary<string, Microcontroller>>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}