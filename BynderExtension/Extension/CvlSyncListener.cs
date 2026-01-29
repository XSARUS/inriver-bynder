using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bynder.Extension
{
    using Api;
    using Utils.Helpers;
    using Enums;
    using Workers;

    /// <summary>
    /// Used to sync CVL values to Bynder
    /// </summary>
    public class CvlSyncListener : Extension, ICVLListener
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = SettingNames.GetDefaultBynderApiSettings();
                settings.Add(Config.Settings.LocaleMappingInriverToBynder, string.Empty);
                settings.Add(Config.Settings.BynderLocaleForMetapropertyOptionLabel, string.Empty);
                settings.Add(Config.Settings.CvlMetapropertyMapping, string.Empty);

                return settings;
            }
        }

        #endregion Properties

        #region Methods
        public void CVLValueCreated(string cvlId, string cvlValueKey)
        {
            try
            {
                var result = Container.GetInstance<CvlSyncWorker>().Execute(CvlAction.Created, cvlId, cvlValueKey);
                if (result.Messages.Count > 0)
                {
                    Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueCreated)} event with CVL id '{cvlId}' and CVL Key '{cvlValueKey}': {string.Join(Environment.NewLine, result.Messages)}");
                }
                else
                {
                    Context.Log(LogLevel.Verbose, $"Finished {nameof(CVLValueCreated)} event with CVL id '{cvlId}' and CVL Key '{cvlValueKey}'; no further information!");
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString(), ex);
            }
        }

        public void CVLValueDeleted(string cvlId, string cvlValueKey)
        {
            try
            {
                var result = Container.GetInstance<CvlSyncWorker>().Execute(CvlAction.Deleted, cvlId, cvlValueKey);
                if (result.Messages.Count > 0)
                {
                    Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueDeleted)} event with CVL id '{cvlId}' and CVL Key '{cvlValueKey}': {string.Join(Environment.NewLine, result.Messages)}");
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString(), ex);
            }
        }

        public void CVLValueDeletedAll(string cvlId)
        {
            try
            {
                var result = Container.GetInstance<CvlSyncWorker>().Execute(CvlAction.DeletedAll, cvlId);
                if (result.Messages.Count > 0)
                {
                    Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueDeletedAll)} event with CVL id '{cvlId}': {string.Join(Environment.NewLine, result.Messages)}");
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString(), ex);
            }
        }

        public void CVLValueUpdated(string cvlId, string cvlValueKey)
        {
            try
            {
                var result = Container.GetInstance<CvlSyncWorker>().Execute(CvlAction.Updated, cvlId, cvlValueKey);
                if (result.Messages.Count > 0)
                {
                    Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueUpdated)} event with CVL id '{cvlId}' and CVL Key '{cvlValueKey}': {string.Join(Environment.NewLine, result.Messages)}");
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString(), ex);
            }
        }

        public override string Test()
        {
            var sb = new StringBuilder();
            try
            {
                sb.AppendLine(base.Test());

                var cvlMetapropertyMapping = SettingHelper.GetCvlMetapropertyMapping(Context.Settings, Context.Logger);
                sb.AppendLine($"CVLs configured: {string.Join(", ", cvlMetapropertyMapping.Keys)}");

                var localeMapping = SettingHelper.GetLocaleMapping(Context.Settings, Context.Logger);
                sb.AppendLine($"Languages configured: {string.Join(", ", localeMapping.Keys)}");
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
