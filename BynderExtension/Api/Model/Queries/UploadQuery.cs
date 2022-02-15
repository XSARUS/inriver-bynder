namespace Bynder.Api.Model
{
    public class UploadQuery
    {
        #region Properties

        /// <summary>
        /// Brand id where we want to store the file
        /// </summary>
        public string BrandId { get; set; }

        /// <summary>
        /// File path of the file we want to update.
        /// </summary>
        public string Filepath { get; set; }

        /// <summary>
        /// Media id. If specified it will add the asset as new version
        /// of the specified media. Otherwise a new media will be added to
        /// the asset bank
        /// </summary>
        public string MediaId { get; set; }

        #endregion Properties
    }
}