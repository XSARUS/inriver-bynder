using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Models
{
    public class FilenameExtensionMediaTypeMapping
    {
        public string FileExtension { get; set; }
        public MediaTypeConfiguration[] MediaTypeConfiguration { get; set; }
    }
}
