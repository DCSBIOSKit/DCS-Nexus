using System.IO;
using DCS_Nexus.Communication;

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
        public CommunicationType SlaveCommunicationType { get; set; } = CommunicationType.Multicast;

        public static Settings Load()
        {
            if (File.Exists("settings.json"))
            {
                string json = File.ReadAllText("settings.json");

                return System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
            }

            return new Settings();
        }

        public void Save()
        {
            string json = System.Text.Json.JsonSerializer.Serialize(this);
            File.WriteAllText("settings.json", json);
        }
    }
}