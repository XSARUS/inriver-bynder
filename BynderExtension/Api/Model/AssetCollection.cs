using System.Collections.Generic;

namespace Bynder.Api.Model
{
    public class AssetCollection
    {
        public List<Asset> Media { get; set; }
        public Total Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }

        /// <summary>
        /// calculate next page number based on current set and parameters
        /// if no next page available return -1
        /// </summary>
        /// <returns></returns>
        public int GetNextPage()
        {
            return !IsLastPage() ? Page + 1 : -1;
        }

        public bool IsLastPage()
        {
            return GetTotal() <= Page * Limit;
        }

        public int GetTotal()
        {
            return Total.Count;
        }
    }

    public class Total
    {
        public int Count { get; set; }
    }
}