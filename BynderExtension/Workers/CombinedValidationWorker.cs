using Bynder.Api;

namespace Bynder.Workers
{
    class CombinedValidationWorker : IWorker
    {
        private readonly ModelValidationWorker _modelValidationWorker;
        private readonly BynderSettingsValidationWorker _bynderSettingsValidationWorker;
        private readonly IBynderClient _bynderClient;

        public CombinedValidationWorker(
            ModelValidationWorker modelValidationWorker,
            BynderSettingsValidationWorker bynderSettingsValidationWorker,
            IBynderClient bynderClient)
        {
            _modelValidationWorker = modelValidationWorker;
            _bynderSettingsValidationWorker = bynderSettingsValidationWorker;
            _bynderClient = bynderClient;
        }

        public WorkerResult Execute()
        {
            var result = new WorkerResult();
            
            result.Messages.AddRange(_modelValidationWorker.Execute().Messages);
            result.Messages.AddRange(_bynderSettingsValidationWorker.Execute().Messages);
            
            // test bynder bynderClient
            result.Messages.Add("Test Bynder API Connection:");
            var account = _bynderClient.GetAccount();
            result.Messages.Add($"Got access to account '{account?.Name}'");

            return result;

        }
    }
}