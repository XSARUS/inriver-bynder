using Bynder.Api;
using Bynder.Api.Model;
using Bynder.Utils.Helpers;
using Bynder.Workers;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Extension
{
    /// <summary>
    /// Used to sync metaproperty options to bynder
    /// </summary>
    public class MetapropertyOptionExportListener : Extension, ICVLListener
    {

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = SettingNames.GetDefaultBynderApiSettings();
                settings.Add(Config.Settings.LocaleMappingInriverToBynder, "");
                return settings;
            }
        }

        public void CVLValueCreated(string cvlId, string cvlValueKey)
        {
            Container.GetInstance<CvlExportWorker>().Execute(CvlAction.Created, cvlId, cvlValueKey);
        }

        public void CVLValueDeleted(string cvlId, string cvlValueKey)
        {
            Container.GetInstance<CvlExportWorker>().Execute(CvlAction.Deleted, cvlId, cvlValueKey);
        }

        public void CVLValueDeletedAll(string cvlId)
        {
            Container.GetInstance<CvlExportWorker>().Execute(CvlAction.DeletedAll, cvlId);
        }

        public void CVLValueUpdated(string cvlId, string cvlValueKey)
        {
            Container.GetInstance<CvlExportWorker>().Execute(CvlAction.Updated, cvlId, cvlValueKey);
        }
    }
    public enum CvlAction
    {
        Created,
        Updated,
        Deleted,
        DeletedAll
    }
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

        private WorkerResult ProcessUpdate(string cvlId, string cvlKey, List<string> metaproperties, Dictionary<string, string> localeMapping)
        {
            var result = new WorkerResult();
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

            return result;
        }

        private WorkerResult ProcessCvlDeletion(string cvlId, List<string> metaproperties)
        {
            var result = new WorkerResult();

            foreach (var metaproperty in metaproperties)
            {
                var options = _bynderClient.GetMetapropertyOptions(metaproperty);

                foreach (var option in options) 
                {
                    _bynderClient.DeleteMetapropertyOption(metaproperty, option.Id);
                }
            }
            
            return result;
        }

        private WorkerResult ProcessDeletion(string cvlId, string cvlKey, List<string> metaproperties)
        {
            var result = new WorkerResult();
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
                foreach (var option in options.Where(x=> x.Label.Equals(cvlKey)))
                {
                    _bynderClient.DeleteMetapropertyOption(metaproperty, option.Id);
                }
            }


            return result;
        }

        private WorkerResult ProcessCreation(string cvlId, string cvlKey, List<string> metaproperties, Dictionary<string,string> localeMapping)
        {
            var result = new WorkerResult();

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
                    // string
                    obj.Label = cvlValue.Value?.ToString() ?? "";
                    obj.Labels = new Dictionary<string, string>
                    {
                        { "", "" }
                    };
                }


                // get original filename, as we need to evaluate this for further processing
                //var asset = _bynderClient.
            }

            return result;
        }
    }
}
