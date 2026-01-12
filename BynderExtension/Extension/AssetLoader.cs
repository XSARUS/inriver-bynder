using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;

namespace Bynder.Extension
{
    using Bynder.Sdk;
    using Bynder.Sdk.Service;
    using Enums;
    using Utils.Helpers;
    using Workers;

    public class AssetLoader : Extension, IScheduledExtension
    {
        #region Methods

        /// <summary>
        /// Get a list of all assetIds from Bynder using the configured filter Query
        /// which will be executed against api/v4/media/?-----
        /// for each found asset, process it using the worker implementation as if it would have been triggered by a notificaton message
        /// </summary>
        public void Execute(bool force)
        {
            // only run manually
            if (!force) return;

            try
            {
                Context.Log(LogLevel.Information, "Start loading assets");

                var worker = Container.GetInstance<AssetUpdatedWorker>();
                var bynderClient = Container.GetInstance<BynderClient>();

                // get all assets ids
                // note: this is a paged result set, call next page until reaching end.
                var counter = 0;
                string query = SettingHelper.GetInitialAssetLoadUrlQuery(DefaultSettings, Context.Logger);
                /// TODO PATRICK: volgens mij is dit niet echt een "Collection" zoals die in Bynder bestaat maar een eigen
                /// interpretatie van een AssetCollection die gevuld wordt door te zoeken naar assets adhv een query??
                /// Dit nog navragen bij Cornelis
                /*var assetCollection = bynderClient.GetAssetCollection(query);
                Context.Log(LogLevel.Information, $"Start processing {assetCollection.GetTotal()} assets.");

                assetCollection.Media.ForEach(a => worker.Execute(a.Id, NotificationType.DataUpsert));
                counter += assetCollection.Media.Count;
                while (!assetCollection.IsLastPage())
                {
                    // when not reached end get next group of assets
                    assetCollection = bynderClient.GetAssetCollection(
                        query,
                        assetCollection.GetNextPage());
                    assetCollection.Media.ForEach(a => worker.Execute(a.Id, NotificationType.DataUpsert));
                    counter += assetCollection.Media.Count;
                    Context.Log(LogLevel.Information, $"Processed {counter} assets.");
                }
                Context.Log(LogLevel.Information, "Initial Import Successful!");*/
            }
            catch (System.Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods
    }
}