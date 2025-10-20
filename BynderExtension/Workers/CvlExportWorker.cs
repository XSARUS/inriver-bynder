using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Workers
{
    using Api;
    using Api.Model;
    using Enums;
    using Utils.Helpers;

    public class CvlExportWorker : IWorker
    {
        #region Fields

        private readonly IBynderClient _bynderClient;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public CvlExportWorker(inRiverContext inRiverContext, IBynderClient bynderClient)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
        }

        #endregion Constructors


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
                var cvlMetapropertyMapping = SettingHelper.GetCvlMetapropertyMapping(_inRiverContext.Settings, _inRiverContext.Logger);
                if (!cvlMetapropertyMapping.ContainsKey(cvlId)) return result;

                var metaproperties = cvlMetapropertyMapping[cvlId];
                if (metaproperties == null || metaproperties.Count == 0)
                {
                    result.Messages.Add($"Metaproperties for CVL '{cvlId}' are not configured!");
                    return result;
                }

                // if no locales, then don't export
                var localeMapping = SettingHelper.GetLocaleMapping(_inRiverContext.Settings, _inRiverContext.Logger);
                if (localeMapping == null) return result;

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
                var cvlValue = _inRiverContext.ExtensionManager.ModelService.GetCVLValueByKey(cvlKey, cvlId);
                if (cvlValue == null)
                {
                    result.Messages.Add($"CVL Value with key '{cvlKey}' of CVL '{cvlId}' does not exist anymore.");
                    return result;
                }

                // export value to each metaproperty it is mapped to
                foreach (var metaproperty in metaproperties)
                {
                    List<MetapropertyOption> options = _bynderClient.GetMetapropertyOptions(metaproperty);

                    // match on label because inriver can have characters which are not allowed in their name in bynder. We use cvlkey as label and as name, so this should work correctly.
                    // only taking the first, if you have more on the same metaproperty, then clean those up in Bynder
                    MetapropertyOption option = options.FirstOrDefault(x => x.Label.Equals(cvlKey));

                    var obj = new MetapropertyOptionPost
                    {
                        Id = option?.Id, // option might not exist at all, then create it, because it is found in inriver
                        ZIndex = cvlValue.Index,
                        IsSelectable = true,
                        Label = cvlKey,
                        Labels = new Dictionary<string, string>()
                    };

                    var ls = cvlValue.Value as LocaleString;
                    if (ls != null)
                    {
                        foreach (var language in ls.Languages)
                        {
                            if (!localeMapping.ContainsKey(language.Name)) continue;

                            obj.Labels[language.Name] = ls[language];
                        }
                    }
                    else
                    {
                        obj.Label = cvlValue.Value?.ToString() ?? "";
                        foreach (var language in ls.Languages)
                        {
                            if (!localeMapping.ContainsKey(language.Name)) continue;

                            obj.Labels[language.Name] = ls[language];
                        }
                    }

                    _bynderClient.SaveMetapropertyOption(metaproperty, obj);
                }
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Error while processing update of option for CVL id '{cvlId}' and CVL key '{cvlKey}'!");
                result.Messages.Add(ex.Message);
            }

            return result;
        }

        private WorkerResult ProcessCvlDeletion(string cvlId, List<string> metaproperties)
        {
            var result = new WorkerResult();

            try
            {
                foreach (var metaproperty in metaproperties)
                {
                    var options = _bynderClient.GetMetapropertyOptions(metaproperty);

                    foreach (var option in options)
                    {
                        _bynderClient.DeleteMetapropertyOption(metaproperty, option.Id);
                    }
                }
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

                foreach (var metaproperty in metaproperties)
                {
                    var options = _bynderClient.GetMetapropertyOptions(metaproperty);

                    // match on label because inriver can have characters which are not allowed in their name in bynder. We use cvlkey as label and as name, so this should work correctly.
                    foreach (var option in options.Where(x => x.Label.Equals(cvlKey)))
                    {
                        _bynderClient.DeleteMetapropertyOption(metaproperty, option.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Error while processing deletion of option for CVL id '{cvlId}' and CVL key '{cvlKey}'!");
                result.Messages.Add(ex.Message);
            }

            return result;
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
                var cvlValue = _inRiverContext.ExtensionManager.ModelService.GetCVLValueByKey(cvlKey, cvlId);
                if (cvlValue == null)
                {
                    result.Messages.Add($"CVL Value with key '{cvlKey}' of CVL '{cvlId}' does not exist anymore.");
                    return result;
                }

                // export value to each metaproperty it is mapped to
                foreach (var metaproperty in metaproperties)
                {
                    var obj = new MetapropertyOptionPost
                    {
                        ZIndex = cvlValue.Index,
                        IsSelectable = true,
                        Name = cvlKey,
                        Label = cvlKey,
                        Labels = new Dictionary<string, string>()
                    };

                    var ls = cvlValue.Value as LocaleString;
                    if (ls != null)
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

                    _bynderClient.SaveMetapropertyOption(metaproperty, obj);
                }
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Error while processing creation of option for CVL id '{cvlId}' and CVL key '{cvlKey}'!");
                result.Messages.Add(ex.Message);
            }

            return result;
        }
    }
}
