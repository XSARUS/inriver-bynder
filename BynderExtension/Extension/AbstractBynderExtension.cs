using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Bynder.Extension
{
    using Api;
    using Utils.Helpers;
    using Enums;
    using Workers;

    public abstract class AbstractBynderExtension: AbstractExtension
    {
        #region Properties
        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = SettingNames.GetDefaultBynderApiSettings();
                foreach (var kvp in base.DefaultSettings)
                {
                    settings[kvp.Key] = kvp.Value;
                }

                return settings;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// test method for extension - called from control panel
        /// </summary>
        /// <returns></returns>
        public override string Test()
        {
            var sb = new StringBuilder();
            try
            {
                if (SettingHelper.ExecuteBaseTestMethod(Context.Settings, Context.Logger))
                {
                    sb.AppendLine(base.Test());
                }
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