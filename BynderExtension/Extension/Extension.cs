using System;
using System.Collections.Generic;
using Bynder.Api;
using Bynder.Workers;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using StructureMap;

namespace Bynder.Extension
{
    public abstract class Extension
    {
        private Container _container;
        protected Container Container => _container ?? (_container = new Container(new Registry(Context)));

        public inRiverContext Context { get; set; }

        public Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = SettingNames.GetDefaultBynderApiSettings();
                settings.Add(Config.Settings.InitialAssetLoadUrlQuery, "type=image");
                settings.Add(Config.Settings.RegularExpressionForFileName, @"^(?<ProductNumber>[0-9a-zA-Z]+)_(?<ResourcePosition>[0-9]+)");
                settings.Add(Config.Settings.MetapropertyMap, "metapropertyguid1=inriverfield1,metapropertyguid2=inriverfield2");
                settings.Add(Config.Settings.AssetPropertyMap, "description=ResourceDescription");
                settings.Add(Config.Settings.InRiverIntegrationId, "41a92562-bfd9-4847-a34d-4320bcef5e4a");
                settings.Add(Config.Settings.InRiverEntityUrl, "https://inriver.productmarketingcloud.com/app/enrich#entity/{entityId}/");
                settings.Add(Config.Settings.BynderBrandName, "");
                settings.Add(Config.Settings.LocaleStringLanguagesToSet, "");
                settings.Add(Config.Settings.MultivalueSeparator, ", ");
                settings.Add(Config.Settings.CreateMissingCvlKeys, true.ToString());
                return settings;
            }
        }

        /// <summary>
        /// test method for extension - called from control panel
        /// </summary>
        /// <returns></returns>
        public string Test()
        {
            var worker = Container.GetInstance<CombinedValidationWorker>();
            var result = worker.Execute();

            // write result to log for more readable access
            result.Messages.ForEach(msg =>
                Context.Logger.Log(msg.ToLower().StartsWith("error") ? LogLevel.Error : LogLevel.Information, msg)
            );

            return string.Join(Environment.NewLine, result.Messages);
        }

    }
}