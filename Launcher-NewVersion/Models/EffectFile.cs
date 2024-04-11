
using Newtonsoft.Json;

namespace Launcher.Models
{
    public class EffectFile
    {
        [JsonProperty("name")]
        public string Name;
        
        [JsonProperty("from")]
        public string From;

        [JsonProperty("to")]
        public string To;
    }
}
