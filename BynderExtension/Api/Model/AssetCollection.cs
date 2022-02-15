using System.Collections.Generic;

namespace Bynder.Api.Model
{
    public class AssetCollection
    {
        #region Properties

        public int Limit { get; set; }
        public List<Asset> Media { get; set; }
        public int Page { get; set; }
        public Total Total { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// calculate next page number based on current set and parameters
        /// if no next page available return -1
        /// </summary>
        /// <returns></returns>
        public int GetNextPage()
        {
            return !IsLastPage() ? Page + 1 : -1;
        }

        public int GetTotal()
        {
            return Total.Count;
        }

        public bool IsLastPage()
        {
            return GetTotal() <= Page * Limit;
        }

        #endregion Methods
    }

    public class Total
    {
        #region Properties

        public int Count { get; set; }

        #endregion Properties
    }
}