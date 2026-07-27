using PropertyManagement.API.Mappers;
using PropertyManagement.API.Models.DTOs.MaintenanceRequests;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using PropertyManagement.API.Repositories;
using PropertyManagement.API.Validators;

namespace PropertyManagement.API.Services
{
    public interface IMaintenanceRequestService
    {
        Task<PagedResponse<MaintenanceRequestListItemResponse>> GetPagedAsync(MaintenanceRequestFilterRequest filter, CancellationToken cancellationToken);
        Task<MaintenanceRequestDetailResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);
        Task<MaintenanceRequestDetailResponse> CreateAsync(CreateMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken);
        Task<MaintenanceRequestDetailResponse> UpdateAsync(long id, UpdateMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken);
        Task ApproveAsync(long id, string performedBy, CancellationToken cancellationToken);
        Task RejectAsync(long id, RejectMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken);
        Task CancelAsync(long id, CancelMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken);
        Task<IReadOnlyList<MaintenanceRequestHistoryResponse>> GetHistoryAsync(long id, CancellationToken cancellationToken);
        Task<IReadOnlyList<MaintenanceRequesterResponse>> GetRequestersAsync(CancellationToken cancellationToken);
    }

    public class MaintenanceRequestService : IMaintenanceRequestService
    {
        private readonly IMaintenanceRequestRepository _repository;

        public MaintenanceRequestService(IMaintenanceRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<MaintenanceRequestListItemResponse>> GetPagedAsync(MaintenanceRequestFilterRequest filter, CancellationToken cancellationToken)
        {
            filter.PageNumber = Math.Max(filter.PageNumber, 1);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 50);

            var (items, totalCount) = await _repository.GetPagedAsync(filter, cancellationToken);

            return new PagedResponse<MaintenanceRequestListItemResponse>
            {
                Items = items.Select(MaintenanceRequestMapper.ToListItem).ToList(),
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        public async Task<MaintenanceRequestDetailResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken);
            return request is null ? null : MaintenanceRequestMapper.ToDetail(request);
        }

        public async Task<MaintenanceRequestDetailResponse> CreateAsync(CreateMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken)
        {
            var errors = MaintenanceRequestValidator.ValidateCreate(request);
            if (errors.Count > 0) throw new MaintenanceRequestValidationException(errors);

            var priority = ParsePriority(request.Priority);
            var title = BuildRequestTitle(request.Title, request.IssueType);
            var description = request.Description?.Trim() ?? string.Empty;

            var requester = await ResolveRequesterAsync(request.RequesterId, request.RequesterName, cancellationToken);
            if (requester is null)
                throw new MaintenanceRequestBusinessException("Requester was not found.");

            var unit = await ResolveUnitAsync(request.UnitId, request.UnitNumber, cancellationToken);
            if (unit is null)
                throw new MaintenanceRequestBusinessException("Property unit was not found.");

            if (await _repository.HasDuplicatePendingSubmissionAsync(requester.Id, unit.Id, title, request.IssueType, description, cancellationToken))
                throw new MaintenanceRequestBusinessException("A matching pending maintenance request already exists.");

            var now = DateTime.UtcNow;
            var entity = new MaintenanceRequest
            {
                RequestNumber = await _repository.GetNextRequestNumberAsync(now.Year, cancellationToken),
                OccupantId = requester.Id,
                UnitId = unit.Id,
                AssetType = request.IssueType.Trim(),
                Title = title,
                Description = description,
                Location = GetRequestLocation(request.Location, unit.UnitNumber),
                ImagePath = request.ImagePath,
                PriorityLevel = priority,
                Status = RequestStatus.Pending,
                PreferredAccessDateTime = request.PreferredAccessDateTime?.ToUniversalTime(),
                RequestDate = now,
                CreatedAt = now,
                CreatedBy = performedBy
            };

            AddHistory(entity, null, RequestStatus.Pending, "Request created", null, performedBy, now);
            await _repository.AddAsync(entity, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var created = await _repository.GetByIdWithHistoryAsync(entity.Id, cancellationToken);
            return MaintenanceRequestMapper.ToDetail(created!);
        }

        public async Task<MaintenanceRequestDetailResponse> UpdateAsync(long id, UpdateMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken)
        {
            var errors = MaintenanceRequestValidator.ValidateUpdate(request);
            if (errors.Count > 0) throw new MaintenanceRequestValidationException(errors);

            var entity = await GetEntityOrThrowAsync(id, cancellationToken);
            if (entity.Status != RequestStatus.Pending)
                throw new MaintenanceRequestBusinessException("Only pending requests may be edited.");

            var unit = await ResolveUnitAsync(request.UnitId, request.UnitNumber, cancellationToken);
            if (unit is null)
                throw new MaintenanceRequestBusinessException("Property unit was not found.");

            entity.UnitId = unit.Id;
            entity.AssetType = request.IssueType.Trim();
            entity.Title = BuildRequestTitle(request.Title, request.IssueType);
            entity.Description = request.Description?.Trim() ?? string.Empty;
            entity.Location = GetRequestLocation(request.Location, unit.UnitNumber);
            entity.PriorityLevel = ParsePriority(request.Priority);
            entity.PreferredAccessDateTime = request.PreferredAccessDateTime?.ToUniversalTime();
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = performedBy;

            AddHistory(entity, RequestStatus.Pending, RequestStatus.Pending, "Request edited", null, performedBy, DateTime.UtcNow);
            await _repository.SaveChangesAsync(cancellationToken);

            var updated = await _repository.GetByIdWithHistoryAsync(id, cancellationToken);
            return MaintenanceRequestMapper.ToDetail(updated!);
        }

        public async Task ApproveAsync(long id, string performedBy, CancellationToken cancellationToken)
        {
            var entity = await GetEntityOrThrowAsync(id, cancellationToken);
            if (entity.Status != RequestStatus.Pending)
                throw new MaintenanceRequestBusinessException("Only pending requests may be approved.");

            var now = DateTime.UtcNow;
            var previousStatus = entity.Status;
            entity.Status = RequestStatus.Approved;
            entity.ApprovedAt = now;
            entity.ApprovedBy = performedBy;
            entity.UpdatedAt = now;
            entity.UpdatedBy = performedBy;
            AddHistory(entity, previousStatus, entity.Status, "Request approved", null, performedBy, now);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task RejectAsync(long id, RejectMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken)
        {
            var errors = MaintenanceRequestValidator.ValidateReason("reason", request.Reason);
            if (errors.Count > 0) throw new MaintenanceRequestValidationException(errors);

            var entity = await GetEntityOrThrowAsync(id, cancellationToken);
            if (entity.Status != RequestStatus.Pending)
                throw new MaintenanceRequestBusinessException("Only pending requests may be rejected.");

            var now = DateTime.UtcNow;
            var previousStatus = entity.Status;
            entity.Status = RequestStatus.Rejected;
            entity.RejectedAt = now;
            entity.RejectedBy = performedBy;
            entity.RejectionReason = request.Reason.Trim();
            entity.UpdatedAt = now;
            entity.UpdatedBy = performedBy;
            AddHistory(entity, previousStatus, entity.Status, "Request rejected", entity.RejectionReason, performedBy, now);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task CancelAsync(long id, CancelMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken)
        {
            var errors = MaintenanceRequestValidator.ValidateReason("reason", request.Reason);
            if (errors.Count > 0) throw new MaintenanceRequestValidationException(errors);

            var entity = await GetEntityOrThrowAsync(id, cancellationToken);
            if (entity.Status != RequestStatus.Pending && entity.Status != RequestStatus.Approved)
                throw new MaintenanceRequestBusinessException("Only pending or approved requests may be cancelled.");

            var now = DateTime.UtcNow;
            var previousStatus = entity.Status;
            entity.Status = RequestStatus.Cancelled;
            entity.CancelledAt = now;
            entity.CancelledBy = performedBy;
            entity.CancellationReason = request.Reason.Trim();
            entity.UpdatedAt = now;
            entity.UpdatedBy = performedBy;
            AddHistory(entity, previousStatus, entity.Status, "Request cancelled", entity.CancellationReason, performedBy, now);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MaintenanceRequestHistoryResponse>> GetHistoryAsync(long id, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity is null) throw new MaintenanceRequestBusinessException("Maintenance request was not found.");

            var history = await _repository.GetHistoryAsync(id, cancellationToken);
            return history.Select(MaintenanceRequestMapper.ToHistory).ToList();
        }

        public Task<IReadOnlyList<MaintenanceRequesterResponse>> GetRequestersAsync(CancellationToken cancellationToken)
        {
            return _repository.GetRequestersAsync(cancellationToken);
        }
        private async Task<Occupant?> ResolveRequesterAsync(long requesterId, string requesterName, CancellationToken cancellationToken)
        {
            if (requesterId > 0)
            {
                return await _repository.OccupantExistsAsync(requesterId, cancellationToken)
                    ? new Occupant { Id = requesterId }
                    : null;
            }

            return string.IsNullOrWhiteSpace(requesterName)
                ? null
                : await _repository.GetOrCreateOccupantByNameAsync(requesterName.Trim(), cancellationToken);
        }
        private async Task<PropertyUnit?> ResolveUnitAsync(long unitId, string unitNumber, CancellationToken cancellationToken)
        {
            if (unitId > 0)
            {
                return await _repository.UnitExistsAsync(unitId, cancellationToken)
                    ? new PropertyUnit { Id = unitId, UnitNumber = unitNumber.Trim().ToUpperInvariant() }
                    : null;
            }

            return string.IsNullOrWhiteSpace(unitNumber)
                ? null
                : await _repository.GetOrCreateUnitByNumberAsync(unitNumber.Trim().ToUpperInvariant(), cancellationToken);
        }
        private static string BuildRequestTitle(string title, string issueType)
        {
            return string.IsNullOrWhiteSpace(title)
                ? $"{issueType.Trim()} request"
                : title.Trim();
        }

        private static string GetRequestLocation(string location, string unitNumber)
        {
            return string.IsNullOrWhiteSpace(location)
                ? unitNumber.Trim().ToUpperInvariant()
                : location.Trim();
        }

        private async Task<MaintenanceRequest> GetEntityOrThrowAsync(long id, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdWithHistoryAsync(id, cancellationToken);
            if (entity is null) throw new MaintenanceRequestBusinessException("Maintenance request was not found.");
            return entity;
        }

        private static PriorityLevel ParsePriority(string value)
        {
            if (!Enum.TryParse<PriorityLevel>(value, true, out var priority) || !Enum.IsDefined(typeof(PriorityLevel), priority))
                throw new MaintenanceRequestBusinessException("Priority is invalid.");
            return priority;
        }

        private static void AddHistory(MaintenanceRequest entity, RequestStatus? previousStatus, RequestStatus newStatus, string action, string? remarks, string performedBy, DateTime performedAt)
        {
            entity.StatusHistories.Add(new MaintenanceRequestStatusHistory
            {
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                Action = action,
                Remarks = remarks,
                PerformedBy = performedBy,
                PerformedAt = performedAt,
                CreatedAt = performedAt
            });
        }
    }
}







