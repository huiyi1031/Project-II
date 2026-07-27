namespace PropertyManagement.API.Models.DTOs.MaintenanceRequests
{
    public class CreateMaintenanceRequestRequest
    {
        public long RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public long UnitId { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public DateTime? PreferredAccessDateTime { get; set; }
        public string? ImagePath { get; set; }
    }

    public class CreateMaintenanceRequestFormRequest
    {
        public long RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public long UnitId { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public DateTime? PreferredAccessDateTime { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }

        public CreateMaintenanceRequestRequest ToCreateRequest(string? imagePath)
        {
            return new CreateMaintenanceRequestRequest
            {
                RequesterId = RequesterId,
                RequesterName = RequesterName,
                UnitId = UnitId,
                UnitNumber = UnitNumber,
                Title = Title,
                IssueType = IssueType,
                Description = Description,
                Location = Location,
                Priority = Priority,
                PreferredAccessDateTime = PreferredAccessDateTime,
                ImagePath = imagePath
            };
        }
    }

    public class UpdateMaintenanceRequestRequest
    {
        public long UnitId { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public DateTime? PreferredAccessDateTime { get; set; }
    }

    public class MaintenanceRequestFilterRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? IssueType { get; set; }
        public string? Priority { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class RejectMaintenanceRequestRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CancelMaintenanceRequestRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class MaintenanceRequestListItemResponse
    {
        public long RequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string RequestTitle { get; set; } = string.Empty;
        public string IssueCategory { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long OccupantID { get; set; }
        public string OccupantName { get; set; } = string.Empty;
        public long UnitID { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string PriorityLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PreferredScheduleDate { get; set; }
        public string? AttachmentPath { get; set; }
    }

    public class MaintenanceRequestDetailResponse : MaintenanceRequestListItemResponse
    {
        public string Description { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectedBy { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelledBy { get; set; }
        public string? CancellationReason { get; set; }
        public List<MaintenanceRequestHistoryResponse> History { get; set; } = new();
    }

    public class MaintenanceRequestHistoryResponse
    {
        public long Id { get; set; }
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
    }

    public class MaintenanceRequesterResponse
    {
        public long OccupantID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string OccupantType { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class PagedResponse<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}


