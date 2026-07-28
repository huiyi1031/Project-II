using System.Text.RegularExpressions;
using PropertyManagement.API.Models.DTOs.MaintenanceRequests;

namespace PropertyManagement.API.Validators
{
    public static class MaintenanceRequestValidator
    {
        private static readonly Regex RequesterNameRegex = new("^[A-Za-z ]+$", RegexOptions.Compiled);
        private static readonly Regex UnitNumberRegex = new("^[A-C]-(0[1-9]|1[0-9]|20)-0[1-9]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IReadOnlyDictionary<string, string[]> ValidateCreate(CreateMaintenanceRequestRequest request)
        {
            return ValidateFields(request.RequesterId, request.RequesterName, request.UnitId, request.UnitNumber, request.Title, request.IssueType, request.Description, request.Location, request.Priority, request.PreferredAccessDateTime);
        }

        public static IReadOnlyDictionary<string, string[]> ValidateUpdate(UpdateMaintenanceRequestRequest request)
        {
            return ValidateFields(1, string.Empty, request.UnitId, request.UnitNumber, request.Title, request.IssueType, request.Description, request.Location, request.Priority, request.PreferredAccessDateTime);
        }

        public static IReadOnlyDictionary<string, string[]> ValidateReason(string fieldName, string value)
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(value))
            {
                errors[fieldName] = new[] { $"{fieldName} is required." };
            }
            else if (value.Trim().Length > 500)
            {
                errors[fieldName] = new[] { $"{fieldName} must not exceed 500 characters." };
            }
            return errors;
        }

        private static Dictionary<string, string[]> ValidateFields(long requesterId, string requesterName, long unitId, string unitNumber, string title, string issueType, string description, string location, string priority, DateTime? preferredAccessDateTime)
        {
            var errors = new Dictionary<string, string[]>();

            ValidateRequester(errors, requesterId, requesterName);
            ValidateUnit(errors, unitId, unitNumber);
            AddOptionalStringError(errors, "title", title, 150);
            AddRequiredStringError(errors, "issueType", issueType, 100);
            AddOptionalStringError(errors, "description", description, 2000);
            AddOptionalStringError(errors, "location", location, 250);
            AddRequiredStringError(errors, "priority", priority, 20);

            if (preferredAccessDateTime.HasValue && preferredAccessDateTime.Value.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-1))
            {
                errors["preferredAccessDateTime"] = new[] { "Preferred access date and time cannot be in the past." };
            }

            return errors;
        }

        private static void ValidateRequester(Dictionary<string, string[]> errors, long requesterId, string requesterName)
        {
            if (requesterId > 0) return;

            if (string.IsNullOrWhiteSpace(requesterName))
            {
                errors["requesterName"] = new[] { "Requester full name is required." };
                return;
            }

            var normalized = requesterName.Trim();
            if (normalized.Length > 100)
            {
                errors["requesterName"] = new[] { "Requester full name must not exceed 100 characters." };
            }
            else if (!RequesterNameRegex.IsMatch(normalized))
            {
                errors["requesterName"] = new[] { "Requester full name can contain letters and spaces only." };
            }
        }

        private static void ValidateUnit(Dictionary<string, string[]> errors, long unitId, string unitNumber)
        {
            if (unitId > 0) return;

            if (string.IsNullOrWhiteSpace(unitNumber))
            {
                errors["unitNumber"] = new[] { "Unit is required." };
                return;
            }

            var normalized = unitNumber.Trim().ToUpperInvariant();
            if (!UnitNumberRegex.IsMatch(normalized))
            {
                errors["unitNumber"] = new[] { "Unit format must be A-C, 01-20, 01-09. Example: A-10-09." };
            }
        }

        private static void AddRequiredStringError(Dictionary<string, string[]> errors, string fieldName, string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors[fieldName] = new[] { $"{fieldName} is required." };
            }
            else if (value.Trim().Length > maxLength)
            {
                errors[fieldName] = new[] { $"{fieldName} must not exceed {maxLength} characters." };
            }
        }

        private static void AddOptionalStringError(Dictionary<string, string[]> errors, string fieldName, string value, int maxLength)
        {
            if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
            {
                errors[fieldName] = new[] { $"{fieldName} must not exceed {maxLength} characters." };
            }
        }
    }
}
