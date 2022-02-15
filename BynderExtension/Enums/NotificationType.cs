namespace Bynder.Enums
{
    public enum NotificationType
    {
        /// <summary>
        /// Upload, create, update
        /// </summary>
        DataUpsert,

        /// <summary>
        /// Only metadata updated. May also create a resource when it does not exist yet in inRiver.
        /// </summary>
        MetadataUpdated,

        /// <summary>
        /// Archived
        /// </summary>
        IsArchived,

        /// <summary>
        /// Deleted
        /// </summary>
        IsDeleted
    }
}