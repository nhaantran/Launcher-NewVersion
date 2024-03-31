
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Launcher.Models
{
    public class MessageBoxContent
    {
        [JsonProperty("Default_Language")]
        public MessageBoxLanguage DefaultLanguage { get; set; }
        [JsonProperty("Data")]
        public List<MessageBoxData> MessageBoxData { get; set; }
    }

    public class MessageBoxData
    {
        [JsonProperty("Title")]
        public MessageBoxTitle Title { get; set; }
        [JsonProperty("Descriptions")]
        public List<MessageBoxDescriptions> MessageBoxDescriptions { get; set; }
    }
    

    public class MessageBoxDescriptions
    {
        [JsonProperty("Message")]
        public string Message { get; set; }
        [JsonProperty("Language")]
        public MessageBoxLanguage Language { get; set; }
    }

    public enum MessageBoxLanguage
    {
        vi,
        en
    }
    
    
}
