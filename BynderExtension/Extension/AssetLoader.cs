using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;

namespace Bynder.Extension
{
    using Bynder.Api.Mappers;
    using Bynder.Sdk;
    using Bynder.Sdk.Model;
    using Bynder.Sdk.Query.Asset;
    using Bynder.Sdk.Service;
    using Enums;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
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

                // get all assets ids
                // note: this is a paged result set, call next page until reaching end.
                var counter = 0;
                var query = GetQuery();

                IReadOnlyList<Media> media = RunSync(() => SearchAndFetchResultAsync(query));
                var worker = Container.GetInstance<AssetUpdatedWorker>();

                Context.Log(LogLevel.Information, $"Start processing {media.Count} assets.");

                foreach (Media medium in media) {
                    worker.Execute(medium.Id, NotificationType.DataUpsert);
                    counter++;
                }

                Context.Log(LogLevel.Information, $"Processed {counter}/{media.Count} assets.");
                Context.Log(LogLevel.Information, "Initial import successful!");
            }
            catch (System.Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        internal MediaQuerySearch GetQuery(bool includeCount = false, bool includeTotal = false)
        {
            string assetLoadUrlQuery = SettingHelper.GetInitialAssetLoadUrlQuery(Context.Settings, Context.Logger);

            var dict = assetLoadUrlQuery
                .Split(',')
                .Select(p => p.Split(new[] { '=' }, 2))
                .ToDictionary(
                    p => p[0],
                    p => p.Length > 1 ? p[1] : null
                );

            var query = ApiQueryMapper.FromDictionary<MediaQuerySearch>(dict);
            query.Count = includeCount;
            query.Total = includeTotal;

            int limit = SettingHelper.GetInitialAssetLoadLimit(Context.Settings, Context.Logger);

            Context.Log(LogLevel.Debug, $"Initial Asset Loader limit: limit to {limit} assets");

            if (limit > 0)
            {
                query.Limit = limit;
                Context.Log(LogLevel.Debug, $"Initial Asset Loader limit: set query limit to {limit} assets");
            }

            return query;
        }

        private static T RunSync<T>(Func<Task<T>> task)
        {
            return Task.Run(task).GetAwaiter().GetResult();
        }

        private async Task<IReadOnlyList<Media>> SearchAndFetchResultAsync(MediaQuery mediaQuery)
        {
            var bynderClient = Container.GetInstance<BynderClient>();
            return await bynderClient
                .GetAssetService()
                .GetAllMediaFullResultAsync(mediaQuery)
                .ConfigureAwait(false);
        }

        private async Task<MediaFullResult> SearchAsync(MediaQuery mediaQuery)
        {
            var bynderClient = Container.GetInstance<BynderClient>();
            return await bynderClient
                .GetAssetService()
                .GetMediaFullResultAsync(mediaQuery)
                .ConfigureAwait(false);
        }

        public override string Test()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.Test());

            try
            {
                var query = GetQuery(true, true);
                MediaFullResult result = RunSync(() => SearchAsync(query));

                sb.AppendLine($"Search resulted in {result.Count.Total} assets in total, without the limit applied.");
                sb.AppendLine($"Search resulted in {result.Media.Count} assets.");
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }

        #endregion Methods
    }
}