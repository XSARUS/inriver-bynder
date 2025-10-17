using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using System;
using System.Collections.Generic;

namespace Bynder.Extension
{
    using Api;
    using Enums;
    using Workers;

    /// <summary>
    /// Used to sync CVL values to Bynder
    /// </summary>
    public class CvlExportListener : Extension, ICVLListener
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = SettingNames.GetDefaultBynderApiSettings();
                settings.Add(Config.Settings.LocaleMappingInriverToBynder, "");
                settings.Add(Config.Settings.CvlMetapropertyMapping, "");
                return settings;
            }
        }

        #endregion Properties

        #region Methods

        public void CVLValueCreated(string cvlId, string cvlValueKey)
        {
            try
            {
                var result = Container.GetInstance<CvlExportWorker>().Execute(CvlAction.Created, cvlId, cvlValueKey);
                Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueCreated)} event with CVL id '{cvlId}' and CVL Key '{cvlValueKey}': {string.Join(Environment.NewLine, result.Messages)}");
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
                var result = Container.GetInstance<CvlExportWorker>().Execute(CvlAction.Deleted, cvlId, cvlValueKey);
                Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueDeleted)} event with CVL id '{cvlId}' and CVL Key '{cvlValueKey}': {string.Join(Environment.NewLine, result.Messages)}");
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
                var result = Container.GetInstance<CvlExportWorker>().Execute(CvlAction.DeletedAll, cvlId);
                Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueDeletedAll)} event with CVL id '{cvlId}': {string.Join(Environment.NewLine, result.Messages)}");
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
                var result = Container.GetInstance<CvlExportWorker>().Execute(CvlAction.Updated, cvlId, cvlValueKey);
                Context.Log(LogLevel.Verbose, $"Result for {nameof(CVLValueUpdated)} event with CVL id '{cvlId}' and CVL Key '{cvlValueKey}': {string.Join(Environment.NewLine, result.Messages)}");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString(), ex);
            }
        }

        #endregion Methods
    }
}
