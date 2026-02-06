using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class FilenameEvaluatorSettingsProvider
    {
        #region Methods

        public static Dictionary<string, string> Create()
        {
            return new Dictionary<string, string>()
            {
                { Settings.RegularExpressionForFileName, string.Empty },
            };
        }

        #endregion Methods
    }
}