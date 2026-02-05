using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Extension
{
    using Api.Mappers;
    using Config;
    using Enums;
    using Sdk;
    using Sdk.Model;
    using Sdk.Query.Asset;
    using Sdk.Service;
    using Utils.Helpers;
    using Workers;
    using SettingProviders;
    using Utils.Extensions;

    public class AssetLoader : AbstractBynderExtension, IScheduledExtension
    {
        #region Methods

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = base.DefaultSettings;
                settings.Add(Settings.InitialAssetLoadUrlQuery, "type=image");
                foreach (var setting in AssetUpdatedWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                return settings;
            }
        }

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
                    var result = worker.Execute(medium.Id, NotificationType.DataUpsert);
                    if (result != null && result.Messages.Any())
                    {
                        result.Messages.ForEach(m => Context.Log(LogLevel.Information, m));
                    }
                    counter++;
                }

                Context.Log(LogLevel.Information, $"Processed {counter}/{media.Count} assets.");
            }
            catch (System.Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }

            Context.Log(LogLevel.Information, "Initial import finished!");
        }

        internal MediaQuerySearch GetQuery(bool includeCount = false, bool includeTotal = false)
        {
            string assetLoadUrlQuery = SettingHelper.GetInitialAssetLoadUrlQuery(Context.Settings, Context.Logger);

            var dict = assetLoadUrlQuery.ToDictionary<string, string>(',', '=');

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

        public AssetUpdatedWorker GetWorker()
        {
            return Container.GetInstance<AssetUpdatedWorker>();
        }

        public override string Test()
        {
            var sb = new StringBuilder();

            try
            {
                if (SettingHelper.ExecuteBaseTestMethod(Context.Settings, Context.Logger))
                {
                    sb.AppendLine(base.Test());
                }

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