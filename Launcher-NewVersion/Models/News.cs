
using Newtonsoft.Json;
using System;

namespace Launcher.Models
{
    public class News
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Slug { get; set; }
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }
        [JsonProperty("published_at")]
        public string PublishedAt { get; set; }
        public string Url { get; set; }
        [JsonProperty("post_title")]
        public string PostTitle { get; set; }
        public string Link { get; set; }
    }
}
