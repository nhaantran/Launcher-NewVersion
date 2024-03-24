using System.Collections.Generic;

namespace Launcher.Models
{
    public class HashSumFileDetail
    {
        public string Path { get; set; }
        public string Hash { get; set; }
        public string State { get; set; }
        public List<DownloadLinkDetail> DownloadLink { get; set; }
    }
}
