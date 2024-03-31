
using Newtonsoft.Json;

namespace Launcher.Models
{
    public class MessageCotent
    {
        [JsonProperty("Default_Language")]
        public string DefaultLanguage { get; set; }
        [JsonProperty("Data")]
        public MessageBoxData MessageBoxData { get; set; }
    }

    public class MessageBoxData
    {
        public string Key { get; set; }
        public string Message { get; set; }
        public string Button { get; set; }
    }
}
