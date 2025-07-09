namespace Bynder.Models
{
    public class MetaPropertyMap
    {
        #region Properties

        /// <summary>
        /// Can be the ID or the Name
        /// </summary>
        public string BynderMetaProperty { get; set; }

        public string InriverFieldTypeId { get; set; }
        public bool IsMultiValue { get; set; } = true;

        #endregion Properties
    }
}