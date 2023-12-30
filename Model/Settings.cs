using System.IO;
using System.Collections.Generic; // Added for List
using DCS_Nexus.Communication;
using static Util.Logger;

namespace DCS_Nexus
{
    public class Settings
    {
        private static Settings defaultInstance = Load();

        public static Settings Default
        {
            get { return defaultInstance; }
        }

        public CommunicationType DCSCommunicationType { get; set; } = CommunicationType.UDP;
        // Changed to a list to support multiple communication types
        public List<CommunicationType> SlaveCommunicationTypes { get; set; } = new List<CommunicationType> { CommunicationType.Multicast };

        public static Settings Load()
        {
            if (File.Exists("settings.json"))
            {
                string json = File.ReadAllText("settings.json");
                var settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);

                return settings ?? new Settings();
            }

            return new Settings();
        }

        public void Save()
        {
            string json = System.Text.Json.JsonSerializer.Serialize(this);
            System.Console.WriteLine($"saved json: {json}");
        }
    }
}
