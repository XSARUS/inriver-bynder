using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class FilenameEvaluatorSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {
            return new Dictionary<string, string>()
            {
                { Settings.RegularExpressionForFileName, string.Empty },
            };
        }
    }
}
