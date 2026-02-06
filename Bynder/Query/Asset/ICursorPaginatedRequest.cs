namespace Bynder.Query.Asset
{
    public interface ICursorPaginatedRequest
    {
        #region Properties

        int? Limit { get; }

        #endregion Properties

        #region Methods

        void SetCursor(string cursor);

        #endregion Methods
    }
}