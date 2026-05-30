using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Milky.OsuPlayer.Common
{
    public class GithubRelease
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
        [JsonProperty("tag_name")]
        public string TagName { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("draft")]
        public bool Draft { get; set; }
        [JsonProperty("prerelease")]
        public bool PreRelease { get; set; }
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonProperty("published_at")]
        public DateTime? PublishedAt { get; set; }
        [JsonProperty("assets")]
        public List<Asset> Assets { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonIgnore]
        public string NewVerString { get; set; }
        [JsonIgnore]
        public string NowVerString { get; set; }
    }

    public class Asset
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
