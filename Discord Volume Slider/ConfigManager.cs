using System.IO;
using Newtonsoft.Json;

namespace DiscordVolumeMixer
{
    public static class ConfigManager
    {
        private static readonly string ConfigFilePath = "config.json";

        public static AppConfig LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<AppConfig>(json);
            }
            return null;
        }

        public static void SaveConfig(AppConfig config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}
