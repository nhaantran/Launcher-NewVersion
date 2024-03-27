
using System.ComponentModel;

namespace Launcher.Models
{
    public enum FileExtentions
    {
        [Description(".zip")]
        Zip,
        
        [Description(".exe")]
        Exe,
        
        [Description(".rar")]
        Rar,

        [Description(".hlzip")]
        HlZip
    }
}
