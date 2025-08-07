namespace Bynder.Workers
{
    using Api;

    internal class CombinedValidationWorker : IWorker
    {
        #region Fields

        private readonly IBynderClient _bynderClient;
        private readonly BynderSettingsValidationWorker _bynderSettingsValidationWorker;
        private readonly ModelValidationWorker _modelValidationWorker;

        #endregion Fields

        #region Constructors

        public CombinedValidationWorker(
            ModelValidationWorker modelValidationWorker,
            BynderSettingsValidationWorker bynderSettingsValidationWorker,
            IBynderClient bynderClient)
        {
            _modelValidationWorker = modelValidationWorker;
            _bynderSettingsValidationWorker = bynderSettingsValidationWorker;
            _bynderClient = bynderClient;
        }

        #endregion Constructors

        #region Methods

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

        #endregion Methods
    }
}