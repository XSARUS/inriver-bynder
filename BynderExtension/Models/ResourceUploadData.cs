namespace Bynder.Models
{
    public sealed class ResourceUploadData
    {
        #region Properties

        public string BrandId { get; set; }
        public byte[] Bytes { get; set; }
        public int FileId { get; set; }
        public string Filename { get; set; }

        #endregion Properties
    }
}