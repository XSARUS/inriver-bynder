namespace Bynder.Workers
{
    using Api;
    using Bynder.Sdk.Model;
    using SdkIBynderClient = Bynder.Sdk.Service.IBynderClient;

    internal class CombinedValidationWorker : IWorker
    {
        #region Fields

        private readonly SdkIBynderClient _bynderClient;
        private readonly BynderSettingsValidationWorker _bynderSettingsValidationWorker;
        private readonly ModelValidationWorker _modelValidationWorker;

        #endregion Fields

        #region Constructors

        public CombinedValidationWorker(
            ModelValidationWorker modelValidationWorker,
            BynderSettingsValidationWorker bynderSettingsValidationWorker,
            SdkIBynderClient bynderClient)
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
            User currentUser = _bynderClient.GetUserService().GetCurrentUserAsync().GetAwaiter().GetResult();
            if (currentUser != null)
            {
                result.Messages.Add($"Got access to current user '{currentUser.Email}'");
                Profile profile = _bynderClient.GetProfileService().GetProfileAsync(new Query.Profile.ProfileQuery() { Id = currentUser.ProfileId }).GetAwaiter().GetResult();
                result.Messages.Add(profile == null? $"No access to the current user's profile!" : $"Got access to current user's profile '{profile.Id}'");
            }
            else
            {
                result.Messages.Add($"No access to current user!");
            }

            return result;
        }

        #endregion Methods
    }
}