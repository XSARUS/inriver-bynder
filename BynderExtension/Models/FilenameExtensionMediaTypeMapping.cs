namespace Bynder.Models
{
    public class FilenameExtensionMediaTypeMapping
    {
        #region Properties

        public string FileExtension { get; set; }
        public MediaTypeConfiguration[] MediaTypeConfiguration { get; set; }

        #endregion Properties
    }
}