namespace Bynder.Api.Model
{
    public class SaveMediaQuery
    {
        #region Properties

        /// <summary>
        /// Brand id we want to save media to
        /// </summary>
        [ApiFieldAttribute("brandid")]
        public string BrandId { get; set; }

        /// <summary>
        /// Name of the asset
        /// </summary>
        [ApiFieldAttribute("name")]
        public string Filename { get; set; }

        /// <summary>
        /// Import id
        /// </summary>
        public string ImportId { get; set; }

        /// <summary>
        /// Media id. If specified it will add the asset as new version
        /// of the specified media. Otherwise a new media will be added to
        /// the asset bank
        /// </summary>
        public string MediaId { get; set; }

        #endregion Properties
    }
}