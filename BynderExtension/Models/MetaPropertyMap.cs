using Bynder.Utils.Extensions;
using System.Text;

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
        public bool UseCvlValue { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Return a generated hash for specific properties so it can be used in a Comparer
        /// </summary>
        public virtual string Hash()
        {
            var input = new StringBuilder();

            input.Append(BynderMetaProperty ?? string.Empty);
            input.Append(InriverFieldTypeId ?? string.Empty);
            input.Append(IsMultiValue.ToString());
            input.Append(UseCvlValue.ToString());

            return input.ToString().ToMd5Hash();
        }

        #endregion Methods
    }
}