using inRiver.Remoting.Extension;
using System;

namespace Bynder.Workers
{
    using Api;
    using Models;

    internal class BynderSettingsValidationWorker : AbstractWorker, IWorker
    {
        #region Fields

        private WorkerResult _workerResult;

        #endregion Fields

        #region Constructors

        public BynderSettingsValidationWorker(inRiverContext inRiverContext) : base(inRiverContext)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// check the neccessary prereqs for the bynder-inriver integration
        /// </summary>
        public WorkerResult Execute()
        {
            // create result object
            _workerResult = new WorkerResult();

            AssumeSettingIsSet(SettingNames.BynderSecretId);
            AssumeSettingIsSet(SettingNames.BynderClientId);
            AssumeSettingIsSet(SettingNames.BynderClientUrl);
            AssumeSettingIsValidUrl(SettingNames.BynderClientUrl);

            return _workerResult;
        }

        private void AddResultLine(string str)
        {
            _workerResult.Messages.Add(str);
        }

        private void AssumeSettingIsSet(string settingKey)
        {
            AddResultLine(InRiverContext.Settings.ContainsKey(settingKey) &&
                          !string.IsNullOrEmpty(InRiverContext.Settings[settingKey])
                ? $"OK: Setting '{settingKey}' is configured"
                : $"ERROR: Setting '{settingKey}' is not configured"
            );
        }

        private void AssumeSettingIsValidUrl(string settingKey)
        {
            bool validUrl = false;
            string url = "";
            if (InRiverContext.Settings.ContainsKey(settingKey))
            {
                url = InRiverContext.Settings[settingKey];
                validUrl = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }

            AddResultLine(validUrl
                ? $"OK: BynderClient URL '{url}' is valid"
                : $"ERROR: BynderClient URL '{url}' is not valid");
        }

        #endregion Methods
    }
}