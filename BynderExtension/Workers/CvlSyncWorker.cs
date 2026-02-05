using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using inRiver.Remoting.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Workers
{
    using Api;
    using Models;
    using SettingProviders;
    using Enums;
    using Sdk.Model;
    using Sdk.Query.Asset;
    using Utils.Extensions;
    using Utils.Helpers;
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    /// <summary>
    /// Used to sync CVL values to Bynder
    /// </summary>
    public class CvlSyncWorker : AbstractBynderWorker, IWorker
    {
        public override Dictionary<string, string> DefaultSettings => CvlSyncWorkerSettingsProvider.Create();

        #region Constructors
        public CvlSyncWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null) : base(inRiverContext, bynderClient)
        {
        }
        #endregion Constructors

        #region Methods

        /// <summary>
        /// Main method of the worker
        /// </summary>
        /// <param name="bynderAssetId"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        public WorkerResult Execute(CvlAction action, string cvlId, string cvlKey = null)
        {
            var result = new WorkerResult();

            try
            {
                // no need to process if not mapped
                var cvlMetapropertyMapping = SettingHelper.GetCvlMetapropertyMapping(InRiverContext.Settings, InRiverContext.Logger);
                if (!cvlMetapropertyMapping.ContainsKey(cvlId)) return result;

                var metaproperties = cvlMetapropertyMapping[cvlId];
                if (metaproperties == null || metaproperties.Count == 0)
                {
                    result.Messages.Add($"Metaproperties for CVL '{cvlId}' are not configured!");
                    return result;
                }

                // if no locales, then don't export
                var localeMapping = SettingHelper.GetLocaleMapping(InRiverContext.Settings, InRiverContext.Logger);
                if (localeMapping.Count == 0)
                {
                    result.Messages.Add($"No locale mapping configured!");
                    return result;
                }

                switch (action)
                {
                    case CvlAction.Created:
                        return ProcessCreation(cvlId, cvlKey, metaproperties, localeMapping);
                    case CvlAction.Updated:
                        return ProcessUpdate(cvlId, cvlKey, metaproperties, localeMapping);
                    case CvlAction.Deleted:
                        return ProcessDeletion(cvlId, cvlKey, metaproperties);
                    case CvlAction.DeletedAll:
                        return ProcessCvlDeletion(cvlId, metaproperties);
                    default:
                        result.Messages.Add($"Action '{action}' not yet supported!");
                        return result;
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(cvlKey))
                {
                    result.Messages.Add($"Error while executing worker for action {action}, CVL id '{cvlId}'!");
                }
                else
                {
                    result.Messages.Add($"Error while executing worker for action {action}, CVL id '{cvlId}' and key '{cvlKey}'!");
                }

                result.Messages.Add(ex.Message);
            }

            return result;
        }

        private MetapropertyOption GetPostData(string cvlKey, Dictionary<string, string> localeMapping, CVLValue cvlValue, string id = null)
        {
            var obj = new MetapropertyOption
            {
                Id = id,
                ZIndex = cvlValue.Index.ToString(),
                IsSelectable = true,
                DisplayLabel = cvlKey.SanitizeBynderName(),
                Name = cvlKey.SanitizeBynderName(),
                Labels = new Dictionary<string, string>()
            };

            if (cvlValue.Value is LocaleString ls)
            {
                foreach (var language in ls.Languages)
                {
                    if (!localeMapping.ContainsKey(language.Name)) continue;

                    string bynderLanguage = localeMapping[language.Name];
                    obj.Labels[bynderLanguage] = ls[language];
                }
            }
            else
            {
                var value = cvlValue.Value?.ToString() ?? "";
                foreach (var bynderLanguage in localeMapping.Values)
                {
                    obj.Labels[bynderLanguage] = value;
                }
            }

            var labelLocale = SettingHelper.GetBynderLocaleForMetapropertyOptionLabel(InRiverContext.Settings, InRiverContext.Logger);
            if (!string.IsNullOrEmpty(labelLocale) && 
                obj.Labels.ContainsKey(labelLocale) && 
                !string.IsNullOrWhiteSpace(obj.Labels[labelLocale]))
            {
                obj.Label = obj.Labels[labelLocale];
            }
            else
            {
                obj.Label = cvlKey;
            }

            return obj;
        }

        private WorkerResult ProcessCreation(string cvlId, string cvlKey, List<string> metaproperties, Dictionary<string, string> localeMapping)
        {
            var result = new WorkerResult();

            try
            {
                if (string.IsNullOrWhiteSpace(cvlId))
                {
                    result.Messages.Add($"CVL id is empty!");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(cvlKey))
                {
                    result.Messages.Add($"CVL key is empty!");
                    return result;
                }

                // check if still exists just to be sure
                var cvlValue = InRiverContext.ExtensionManager.ModelService.GetCVLValueByKey(cvlKey, cvlId);
                if (cvlValue == null)
                {
                    result.Messages.Add($"CVL Value with key '{cvlKey}' of CVL '{cvlId}' does not exist anymore.");
                    return result;
                }

                // export value to each metaproperty it is mapped to
                foreach (var metapropertyId in metaproperties)
                {
                    MetapropertyOption obj = GetPostData(cvlKey, localeMapping, cvlValue);
                    _bynderClient.GetAssetService().UpsertMetapropertyOptionAsync(metapropertyId, obj).GetAwaiter().GetResult();
                }

                result.Messages.Add($"Succesfully created the option for CVL '{cvlId}' and key '{cvlKey}' in Bynder!");
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Error while processing creation of option for CVL id '{cvlId}' and CVL key '{cvlKey}'!");
                result.Messages.Add(ex.Message);
            }

            return result;
        }

        private WorkerResult ProcessCvlDeletion(string cvlId, List<string> metaproperties)
        {
            var result = new WorkerResult();

            try
            {
                foreach (var metapropertyId in metaproperties)
                {
                    var options = _bynderClient.GetAssetService().GetMetapropertyOptionsAsync(metapropertyId, new MetapropertyOptionQuery()).GetAwaiter().GetResult();

                    foreach (var option in options)
                    {
                        _bynderClient.GetAssetService().DeleteMetapropertyOptionAsync(metapropertyId, option.Id).GetAwaiter().GetResult();
                    }
                }

                result.Messages.Add($"Succesfully deleted the options for CVL '{cvlId}' in Bynder!");
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Error while processing deletion of options for CVL id '{cvlId}'!");
                result.Messages.Add(ex.Message);
            }

            return result;
        }

        private WorkerResult ProcessDeletion(string cvlId, string cvlKey, List<string> metaproperties)
        {
            var result = new WorkerResult();

            try
            {
                if (string.IsNullOrWhiteSpace(cvlId))
                {
                    result.Messages.Add($"CVL id is empty!");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(cvlKey))
                {
                    result.Messages.Add($"CVL key is empty!");
                    return result;
                }

                var sanitizedName = cvlKey.SanitizeBynderName();

                foreach (var metapropertyId in metaproperties)
                {
                    var options = _bynderClient.GetAssetService().GetMetapropertyOptionsAsync(metapropertyId, new MetapropertyOptionQuery()).GetAwaiter().GetResult();

                    // match on a sanitized name. Matchin on Label is not possible with the CVL key, because that one will be overwritten by a value in the Labels (over time).
                    foreach (var option in options.Where(x => x.Name.Equals(sanitizedName)))
                    {
                        _bynderClient.GetAssetService().DeleteMetapropertyOptionAsync(metapropertyId, option.Id).GetAwaiter().GetResult();
                    }
                }

                result.Messages.Add($"Succesfully deleted the option of CVL '{cvlId}' and key '{cvlKey}' in Bynder!");
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Error while processing deletion of option for CVL id '{cvlId}' and CVL key '{cvlKey}'!");
                result.Messages.Add(ex.Message);
            }

            return result;
        }

        private WorkerResult ProcessUpdate(string cvlId, string cvlKey, List<string> metaproperties, Dictionary<string, string> localeMapping)
        {
            var result = new WorkerResult();
            try
            {
                if (string.IsNullOrWhiteSpace(cvlId))
                {
                    result.Messages.Add($"CVL id is empty!");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(cvlKey))
                {
                    result.Messages.Add($"CVL key is empty!");
                    return result;
                }

                // check if still exists just to be sure
                var cvlValue = InRiverContext.ExtensionManager.ModelService.GetCVLValueByKey(cvlKey, cvlId);
                if (cvlValue == null)
                {
                    result.Messages.Add($"CVL Value with key '{cvlKey}' of CVL '{cvlId}' does not exist anymore.");
                    return result;
                }

                var sanitizedName = cvlKey.SanitizeBynderName();

                // export value to each metaproperty it is mapped to
                foreach (var metapropertyId in metaproperties)
                {
                    IEnumerable<MetapropertyOption> options = _bynderClient.GetAssetService().GetMetapropertyOptionsAsync(metapropertyId, new MetapropertyOptionQuery()).GetAwaiter().GetResult();

                    // match on label because inriver can have characters which are not allowed in their name in bynder. We use cvlkey as label and as name, so this should work correctly.
                    // only taking the first, if you have more on the same metaproperty, then clean those up in Bynder
                    MetapropertyOption option = options.FirstOrDefault(x => x.Name.Equals(sanitizedName));

                    // option might not exist at all, then create it, because it is found in inriver
                    MetapropertyOption obj = GetPostData(cvlKey, localeMapping, cvlValue, option?.Id);
                    _bynderClient.GetAssetService().UpsertMetapropertyOptionAsync(metapropertyId, obj).GetAwaiter().GetResult();
                }

                result.Messages.Add($"Succesfully updated the option for CVL '{cvlId}' and key '{cvlKey}' in Bynder!");
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Error while processing update of option for CVL id '{cvlId}' and CVL key '{cvlKey}'!");
                result.Messages.Add(ex.Message);
            }

            return result;
        }

        #endregion Methods
    }
}
