
using Newtonsoft.Json;

namespace Launcher.Models
{
    public class Mirror
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("testfile")]
        public string TestFile { get; set; }
    }
}
