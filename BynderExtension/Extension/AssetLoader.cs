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
                string assetLoadUrlQuery = SettingHelper.GetInitialAssetLoadUrlQuery(DefaultSettings, Context.Logger);

                var dict = assetLoadUrlQuery
                    .Split(',')
                    .Select(p => p.Split(new[] { '=' }, 2))
                    .ToDictionary(
                        p => p[0],
                        p => p.Length > 1 ? p[1] : null
                    );

                var query = ApiQueryMapper.FromDictionary<MediaQuerySearch>(dict);
                query.Count = false;
                query.Total = false;

                IReadOnlyList<Media> media = RunSync(() => SearchAsync(query));
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

        private static T RunSync<T>(Func<Task<T>> task)
        {
            return Task.Run(task).GetAwaiter().GetResult();
        }

        private async Task<IReadOnlyList<Media>> SearchAsync(MediaQuery mediaQuery)
        {
            var bynderClient = Container.GetInstance<BynderClient>();
            return await bynderClient
                .GetAssetService()
                .GetAllMediaFullResultAsync(mediaQuery)
                //.GetMediaFullResultAsync(mediaQuery)
                .ConfigureAwait(false);
        }

        #endregion Methods
    }
}