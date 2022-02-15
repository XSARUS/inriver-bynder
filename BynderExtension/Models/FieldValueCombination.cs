namespace Bynder.Models
{
    public class FieldValueCombination
    {
        #region Properties

        public string FieldTypeId { get; set; }

        /// <summary>
        /// If true, it sets a timestamp. Has to be a DateTime field. Local or UTC will be configured for the full extension, not per fieldtype.
        /// Default false.
        /// </summary>
        public bool SetTimestamp { get; set; } = false;

        /// <summary>
        /// Use ConvertTo on this, so it will be converted to the correct datatype of the field. Leave empty when using timestamp.
        /// </summary>
        public object Value { get; set; }

        #endregion Properties
    }
}