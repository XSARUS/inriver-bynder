﻿using Bynder.Names;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;

namespace Bynder.Workers
{
    public class ModelValidationWorker : IWorker
    {
        #region Fields

        private readonly inRiverContext _inRiverContext;
        private WorkerResult _workerResult;

        #endregion Fields

        #region Constructors

        public ModelValidationWorker(inRiverContext inRiverContext)
        {
            _inRiverContext = inRiverContext;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// check the necesaary prereqs for the bynder-inriver integration in the inriver model
        /// </summary>
        public WorkerResult Execute()
        {
            // create result object
            _workerResult = new WorkerResult();

            // check existance of resource fields
            AssumeFieldTypeExists(FieldTypeIds.ResourceBynderId);
            AssumeFieldTypeExists(FieldTypeIds.ResourceBynderIdHash);
            AssumeFieldTypeExists(FieldTypeIds.ResourceBynderDownloadState);
            AssumeFieldTypeExists(FieldTypeIds.ResourceFileId);
            AssumeFieldTypeExists(FieldTypeIds.ResourceFilename);
            AssumeFieldTypeExists(FieldTypeIds.ResourceMimeType);

            // check existance of CVL
            AssumeCVLExists(CvlIds.ResourceBynderState);
            AssumeCVLValuesExists(CvlIds.ResourceBynderState, new[] { BynderStates.Todo, BynderStates.Done, BynderStates.Error });

            // check if field downloadstate linked to right CVL
            AssumeFieldTypeIsCVL(FieldTypeIds.ResourceBynderDownloadState, CvlIds.ResourceBynderState);

            return _workerResult;
        }

        private void AddResultLine(string str)
        {
            _workerResult.Messages.Add(str);
        }

        private void AssumeCVLExists(string id)
        {
            var cvl = _inRiverContext.ExtensionManager.ModelService.GetCVL(id);
            AddResultLine(cvl == null
                ? $"ERROR: CVL '{id}' does not exist in model."
                : $"OK: CVL '{id}' exists in model.");
        }

        private void AssumeCVLValuesExists(string id, string[] values)
        {
            var cvlValues = _inRiverContext.ExtensionManager.ModelService.GetCVLValuesForCVL(id);
            foreach (var value in values)
            {
                if (cvlValues == null || !cvlValues.Exists(v => v.Key.Equals(value)))
                {
                    AddResultLine($"ERROR: CVLValue '{id}':'{value}' does not exist in model.");
                }
                else
                {
                    AddResultLine($"OK: CVLValue '{id}':'{value}' exists in model.");
                }
            }
        }

        private void AssumeFieldTypeExists(string id)
        {
            var fieldType = _inRiverContext.ExtensionManager.ModelService.GetFieldType(id);
            AddResultLine(fieldType == null
                ? $"ERROR: FieldType '{id}' does not exist in model."
                : $"OK: FieldType '{id}' exists in model.");
        }

        private void AssumeFieldTypeIsCVL(string fieldTypeId, string cvlId)
        {
            var fieldType = _inRiverContext.ExtensionManager.ModelService.GetFieldType(fieldTypeId);
            if (fieldType == null
                || fieldType.DataType != DataType.CVL
                || fieldType.CVLId != cvlId)
            {
                AddResultLine($"ERROR: FieldType '{fieldTypeId}' is not of type CVL with cvlId '{cvlId}'");
            }
            else
            {
                AddResultLine($"OK: FieldType '{fieldTypeId}' is of type CVL with cvlId '{cvlId}'");
            }
        }

        #endregion Methods
    }
}