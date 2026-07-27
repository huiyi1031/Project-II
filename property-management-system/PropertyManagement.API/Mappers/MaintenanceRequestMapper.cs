using PropertyManagement.API.Models.DTOs.MaintenanceRequests;
using PropertyManagement.API.Models.Entities;

namespace PropertyManagement.API.Mappers
{
    public static class MaintenanceRequestMapper
    {
        public static MaintenanceRequestListItemResponse ToListItem(MaintenanceRequest request)
        {
            return new MaintenanceRequestListItemResponse
            {
                RequestID = request.Id,
                RequestNumber = request.RequestNumber,
                RequestTitle = request.Title,
                IssueCategory = request.AssetType ?? string.Empty,
                Location = request.Location,
                OccupantID = request.OccupantId,
                OccupantName = request.Occupant?.FullName ?? string.Empty,
                UnitID = request.UnitId,
                UnitNumber = request.PropertyUnit?.UnitNumber ?? string.Empty,
                PriorityLevel = request.PriorityLevel.ToString(),
                Status = request.Status.ToString(),
                SubmissionDate = request.RequestDate,
                CreatedAt = request.CreatedAt,
                PreferredScheduleDate = request.PreferredAccessDateTime,
                AttachmentPath = request.ImagePath
            };
        }

        public static MaintenanceRequestDetailResponse ToDetail(MaintenanceRequest request)
        {
            var response = new MaintenanceRequestDetailResponse
            {
                RequestID = request.Id,
                RequestNumber = request.RequestNumber,
                RequestTitle = request.Title,
                IssueCategory = request.AssetType ?? string.Empty,
                Description = request.Description,
                Location = request.Location,
                OccupantID = request.OccupantId,
                OccupantName = request.Occupant?.FullName ?? string.Empty,
                UnitID = request.UnitId,
                UnitNumber = request.PropertyUnit?.UnitNumber ?? string.Empty,
                PriorityLevel = request.PriorityLevel.ToString(),
                Status = request.Status.ToString(),
                SubmissionDate = request.RequestDate,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                CreatedBy = request.CreatedBy,
                UpdatedBy = request.UpdatedBy,
                PreferredScheduleDate = request.PreferredAccessDateTime,
                ApprovedAt = request.ApprovedAt,
                ApprovedBy = request.ApprovedBy,
                RejectedAt = request.RejectedAt,
                RejectedBy = request.RejectedBy,
                RejectionReason = request.RejectionReason,
                CancelledAt = request.CancelledAt,
                CancelledBy = request.CancelledBy,
                CancellationReason = request.CancellationReason,
                History = request.StatusHistories
                    .OrderByDescending(history => history.PerformedAt)
                    .Select(ToHistory)
                    .ToList()
            };

            return response;
        }

        public static MaintenanceRequestHistoryResponse ToHistory(MaintenanceRequestStatusHistory history)
        {
            return new MaintenanceRequestHistoryResponse
            {
                Id = history.Id,
                PreviousStatus = history.PreviousStatus?.ToString(),
                NewStatus = history.NewStatus.ToString(),
                Action = history.Action,
                Remarks = history.Remarks,
                PerformedBy = history.PerformedBy,
                PerformedAt = history.PerformedAt
            };
        }
    }
}
