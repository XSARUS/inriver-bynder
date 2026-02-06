using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bynder.Extension
{
    using Workers;

    public abstract class AbstractExtension
    {
        #region Fields

        protected Container _container;

        #endregion Fields

        #region Properties

        public inRiverContext Context { get; set; }

        public virtual Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = new Dictionary<string, string>
                {
                    { Config.Settings.ExecuteBaseTestMethod, true.ToString() }
                };

                return settings;
            }
        }

        protected Container Container => _container ?? (_container = new Container(new Registry(Context)));

        #endregion Properties

        #region Methods

        /// <summary>
        /// test method for extension - called from control panel
        /// </summary>
        /// <returns></returns>
        public virtual string Test()
        {
            var sb = new StringBuilder();
            try
            {
                var worker = Container.GetInstance<CombinedValidationWorker>();
                var result = worker.Execute();

                // write result to log for more readable access
                result.Messages.ForEach(msg =>
                {
                    sb.AppendLine(msg);
                    Context.Log(msg.ToLower().StartsWith("error") ? LogLevel.Error : LogLevel.Information, msg);
                });
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