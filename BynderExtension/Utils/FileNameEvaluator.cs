
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bynder.Utils
{
    using Helpers;
    using Names;

    public class FilenameEvaluator
    {
        #region Fields

        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public FilenameEvaluator(inRiverContext inRiverContext)
        {
            _inRiverContext = inRiverContext;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// evaluate fileName based on settings
        /// </summary>
        public Result Evaluate(string fileName)
        {
            var result = new Result { Filename = fileName };

            string regularExpressionPattern = SettingHelper.GetRegularExpressionForFileName(_inRiverContext.Settings, _inRiverContext.Logger);
            var regex = new Regex(regularExpressionPattern, RegexOptions.None);

            result.Match = regex.Match(fileName);
            _inRiverContext.Logger.Log(LogLevel.Debug, $"Matching file '{fileName}', found: '{result.Match.Value}'");

            for (int i = 1; i < result.Match.Groups.Count; i++)
            {
                // check if matchgroup name indicates a non resource field, if so, add to output collection
                var groupName = regex.GroupNameFromNumber(i);
                var fieldType = _inRiverContext.ExtensionManager.ModelService.GetFieldType(groupName);

                if (fieldType == null) continue;
                if (string.IsNullOrWhiteSpace(result?.Match?.Groups[i]?.Value)) continue;

                result.EntityDataInFilename.Add(fieldType, result.Match.Groups[i].Value);
            }

            return result;
        }

        #endregion Methods

        #region Classes

        public class Result
        {
            #region Properties

            public Dictionary<FieldType, string> EntityDataInFilename { get; set; }
            public string Filename { get; set; }
            public Match Match { get; set; }

            #endregion Properties

            #region Constructors

            public Result()
            {
                EntityDataInFilename = new Dictionary<FieldType, string>();
            }

            #endregion Constructors

            #region Methods

            public string GetLinkSourceEntityId()
            {
                return string.Empty;
            }

            public Dictionary<string, string> GetLinkType()
            {
                return new Dictionary<string, string>();
            }

            public Dictionary<FieldType, string> GetRelatedEntityDataInFilename()
            {
                return EntityDataInFilename.Where(kv => kv.Key.EntityTypeId != EntityTypeIds.Resource)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            public Dictionary<FieldType, string> GetResourceDataInFilename()
            {
                return EntityDataInFilename.Where(kv => kv.Key.EntityTypeId == EntityTypeIds.Resource)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            public bool IsMatch() => Match.Success;

            #endregion Methods
        }

        #endregion Classes
    }
}