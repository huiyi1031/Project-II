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
        Task ApproveAsync(long id, long performedById, string performedBy, CancellationToken cancellationToken);
        Task TechnicianAcceptAsync(long id, long technicianId, string performedBy, CancellationToken cancellationToken);
        Task ScheduleAsync(long id, ScheduleRequestDto request, string performedBy, CancellationToken cancellationToken);
        Task RejectAsync(long id, RejectMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken);
        Task CancelAsync(long id, CancelMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken);
        Task<IReadOnlyList<MaintenanceRequestHistoryResponse>> GetHistoryAsync(long id, CancellationToken cancellationToken);
        Task<IReadOnlyList<MaintenanceRequesterResponse>> GetRequestersAsync(CancellationToken cancellationToken);
    }

    public class MaintenanceRequestService : IMaintenanceRequestService
    {
        private readonly IMaintenanceRequestRepository _repository;
        private readonly PropertyManagement.API.Data.AppDbContext _context;

        public MaintenanceRequestService(IMaintenanceRequestRepository repository, PropertyManagement.API.Data.AppDbContext context)
        {
            _repository = repository;
            _context = context;
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
            
            var createdEntity = await _repository.GetByIdWithHistoryAsync(entity.Id, cancellationToken);

            // Auto-create chatroom and add occupants linked to the unit
            var chat = new PropertyManagement.API.Models.Entities.Chat
            {
                RequestId = entity.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Only add the requester as Admin
            var requesterEntity = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.Occupants, o => o.Id == requester.Id, cancellationToken);

            if (requesterEntity != null && requesterEntity.UserAccountId > 0)
            {
                chat.Participants.Add(new PropertyManagement.API.Models.Entities.ChatParticipant
                {
                    UserAccountId = requesterEntity.UserAccountId,
                    IsAdmin = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                // Auto-add the relevant owner to the chatroom if the requester is a tenant or family member (resident)
                if ((requesterEntity.OccupantType == OccupantType.Tenant || requesterEntity.OccupantType == OccupantType.Resident) && requesterEntity.ParentOccupantId.HasValue)
                {
                    var ownerEntity = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                        _context.Occupants, o => o.Id == requesterEntity.ParentOccupantId.Value, cancellationToken);
                        
                    if (ownerEntity != null && ownerEntity.UserAccountId > 0)
                    {
                        chat.Participants.Add(new PropertyManagement.API.Models.Entities.ChatParticipant
                        {
                            UserAccountId = ownerEntity.UserAccountId,
                            IsAdmin = false,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }
            }

            // Auto-add property managers based on PropertyType
            var propertyUnit = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(_context.PropertyUnits, u => u.Property), 
                u => u.Id == unit.Id, cancellationToken);
                
            if (propertyUnit?.Property != null && !string.IsNullOrEmpty(propertyUnit.Property.PropertyType))
            {
                var propertyType = propertyUnit.Property.PropertyType;
                var managers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                    System.Linq.Queryable.Where(_context.PropertyManagers, pm => pm.Position == propertyType && pm.UserAccountId > 0), 
                    cancellationToken);
                    
                foreach (var manager in managers)
                {
                    chat.Participants.Add(new PropertyManagement.API.Models.Entities.ChatParticipant
                    {
                        UserAccountId = manager.UserAccountId,
                        IsAdmin = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync(cancellationToken);

            return MaintenanceRequestMapper.ToDetail(createdEntity!);
        }

        public async Task<MaintenanceRequestDetailResponse> UpdateAsync(long id, UpdateMaintenanceRequestRequest request, string performedBy, CancellationToken cancellationToken)
        {
            var errors = MaintenanceRequestValidator.ValidateUpdate(request);
            if (errors.Count > 0) throw new MaintenanceRequestValidationException(errors);

            var entity = await GetEntityOrThrowAsync(id, cancellationToken);
            if (entity.Status != RequestStatus.Pending && entity.Status != RequestStatus.Approved)
                throw new MaintenanceRequestBusinessException("Only pending or approved requests may be edited.");

            if (entity.Status == RequestStatus.Pending)
            {
                var unit = await ResolveUnitAsync(request.UnitId, request.UnitNumber, cancellationToken);
                if (unit is null)
                    throw new MaintenanceRequestBusinessException("Property unit was not found.");

                entity.UnitId = unit.Id;
                entity.AssetType = request.IssueType.Trim();
                entity.Title = BuildRequestTitle(request.Title, request.IssueType);
                entity.Location = GetRequestLocation(request.Location, unit.UnitNumber);
                entity.PriorityLevel = ParsePriority(request.Priority);
                entity.PreferredAccessDateTime = request.PreferredAccessDateTime?.ToUniversalTime();
            }

            entity.Description = request.Description?.Trim() ?? string.Empty;
            
            if (!string.IsNullOrEmpty(request.ImagePath))
            {
                entity.ImagePath = request.ImagePath;
            }
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = performedBy;

            AddHistory(entity, entity.Status, entity.Status, "Request edited", null, performedBy, DateTime.UtcNow);
            await _repository.SaveChangesAsync(cancellationToken);

            var updated = await _repository.GetByIdWithHistoryAsync(id, cancellationToken);
            return MaintenanceRequestMapper.ToDetail(updated!);
        }

        public async Task ApproveAsync(long id, long performedById, string performedBy, CancellationToken cancellationToken)
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
            
            // Add manager to chat
            var chat = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.Chats, c => c.RequestId == entity.Id, cancellationToken);
            
            if (chat != null && performedById > 0)
            {
                var participant = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    System.Linq.Queryable.Where(_context.ChatParticipants, cp => cp.ChatId == chat.Id && cp.UserAccountId == performedById), cancellationToken);
                
                if (participant == null)
                {
                    _context.ChatParticipants.Add(new PropertyManagement.API.Models.Entities.ChatParticipant
                    {
                        ChatId = chat.Id,
                        UserAccountId = performedById,
                        IsAdmin = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                else
                {
                    participant.IsAdmin = true;
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task TechnicianAcceptAsync(long id, long technicianId, string performedBy, CancellationToken cancellationToken)
        {
            var entity = await GetEntityOrThrowAsync(id, cancellationToken);
            if (entity.Status != RequestStatus.Approved)
                throw new MaintenanceRequestBusinessException("Only approved requests may be accepted by a technician.");

            var now = DateTime.UtcNow;
            var previousStatus = entity.Status;
            entity.Status = RequestStatus.Scheduling;
            entity.UpdatedAt = now;
            entity.UpdatedBy = performedBy;
            AddHistory(entity, previousStatus, entity.Status, "Technician accepted request", null, performedBy, now);
            
            // Add technician to chat
            var chat = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.Chats, c => c.RequestId == entity.Id, cancellationToken);
            
            if (chat != null && technicianId > 0)
            {
                var exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                    System.Linq.Queryable.Where(_context.ChatParticipants, cp => cp.ChatId == chat.Id && cp.UserAccountId == technicianId), cancellationToken);
                
                if (!exists)
                {
                    _context.ChatParticipants.Add(new PropertyManagement.API.Models.Entities.ChatParticipant
                    {
                        ChatId = chat.Id,
                        UserAccountId = technicianId,
                        IsAdmin = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task ScheduleAsync(long id, ScheduleRequestDto request, string performedBy, CancellationToken cancellationToken)
        {
            var entity = await GetEntityOrThrowAsync(id, cancellationToken);
            if (entity.Status != RequestStatus.Scheduling)
                throw new MaintenanceRequestBusinessException("Only requests in Scheduling state can be Scheduled.");

            var now = DateTime.UtcNow;
            var previousStatus = entity.Status;
            entity.Status = RequestStatus.Scheduled;
            entity.ScheduledDate = request.ScheduledDate.ToUniversalTime();
            entity.UpdatedAt = now;
            entity.UpdatedBy = performedBy;
            AddHistory(entity, previousStatus, entity.Status, "Maintenance date and time scheduled", null, performedBy, now);

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







