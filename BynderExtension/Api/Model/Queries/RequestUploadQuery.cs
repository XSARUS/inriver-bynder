namespace Bynder.Api.Model
{
    public class RequestUploadQuery
    {
        #region Properties

        /// <summary>
        /// Filename of the file we want to initialize the upload
        /// </summary>
        [ApiFieldAttribute("filename")]
        public string Filename { get; set; }

        #endregion Properties
    }
}