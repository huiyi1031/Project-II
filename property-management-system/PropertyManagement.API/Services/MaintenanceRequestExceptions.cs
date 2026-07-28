namespace PropertyManagement.API.Services
{
    public class MaintenanceRequestValidationException : Exception
    {
        public MaintenanceRequestValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base("Validation failed.")
        {
            Errors = errors;
        }

        public IReadOnlyDictionary<string, string[]> Errors { get; }
    }

    public class MaintenanceRequestBusinessException : Exception
    {
        public MaintenanceRequestBusinessException(string message)
            : base(message)
        {
        }
    }
}
