using System;
using Bynder.Api;
using inRiver.Remoting.Extension;

namespace Bynder.Workers
{
    class BynderSettingsValidationWorker : IWorker
    {
        private readonly inRiverContext _inRiverContext;
        private WorkerResult _workerResult;

        public BynderSettingsValidationWorker(inRiverContext inRiverContext)
        {
            _inRiverContext = inRiverContext;
        }

        /// <summary>
        /// check the necesaary prereqs for the bynder-inriver integration
        /// </summary>
        public WorkerResult Execute()
        {
            // create result object
            _workerResult = new WorkerResult();

            AssumeSettingIsSet(SettingNames.ConsumerSecret);
            AssumeSettingIsSet(SettingNames.ConsumerKey);
            AssumeSettingIsSet(SettingNames.Token);
            AssumeSettingIsSet(SettingNames.TokenSecret);
            AssumeSettingIsSet(SettingNames.CustomerBynderUrl);
            AssumeSettingIsValidUrl(SettingNames.CustomerBynderUrl);

            return _workerResult;
        }

        private void AddResultLine(string str)
        {
            _workerResult.Messages.Add(str);
        }

        private void AssumeSettingIsSet(string settingKey)
        {
            AddResultLine(_inRiverContext.Settings.ContainsKey(settingKey) &&
                          !string.IsNullOrEmpty(_inRiverContext.Settings[settingKey])
                ? $"OK: Setting '{settingKey}' is configured"
                : $"ERROR: Setting '{settingKey}' is not configured"
            );
        }

        private void AssumeSettingIsValidUrl(string settingKey)
        {
            bool validUrl = false;
            string url = "";
            if (_inRiverContext.Settings.ContainsKey(settingKey))
            {
                url = _inRiverContext.Settings[settingKey];
                validUrl = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }

            AddResultLine(validUrl
                ? $"OK: BynderClient URL '{url}' is valid"
                : $"ERROR: BynderClient URL '{url}' is not valid");
        }

    }
}