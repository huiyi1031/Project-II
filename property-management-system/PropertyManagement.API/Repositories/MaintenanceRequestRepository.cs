using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.DTOs.MaintenanceRequests;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Repositories
{
    public class MaintenanceRequestRepository : IMaintenanceRequestRepository
    {
        private readonly AppDbContext _context;

        public MaintenanceRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<MaintenanceRequest> Items, int TotalCount)> GetPagedAsync(MaintenanceRequestFilterRequest filter, CancellationToken cancellationToken)
        {
            var query = _context.MaintenanceRequests
                .AsNoTracking()
                .Include(request => request.Occupant)
                .Include(request => request.PropertyUnit)
                    .ThenInclude(unit => unit!.Property)
                .Where(request => !request.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(request =>
                    request.RequestNumber.ToLower().Contains(search) ||
                    request.Title.ToLower().Contains(search) ||
                    (request.AssetType != null && request.AssetType.ToLower().Contains(search)) ||
                    request.Location.ToLower().Contains(search) ||
                    (request.Occupant != null && request.Occupant.FullName.ToLower().Contains(search)));
            }

            if (TryParseStatus(filter.Status, out var statusList))
            {
                query = query.Where(request => statusList.Contains(request.Status));
            }

            if (!string.IsNullOrWhiteSpace(filter.IssueType))
            {
                var issueType = filter.IssueType.Trim().ToLower();
                query = query.Where(request => request.AssetType != null && request.AssetType.ToLower() == issueType);
            }

            if (TryParsePriority(filter.Priority, out var priority))
            {
                query = query.Where(request => request.PriorityLevel == priority);
            }

            if (filter.CreatedFrom.HasValue)
            {
                var createdFrom = filter.CreatedFrom.Value.Date.ToUniversalTime();
                query = query.Where(request => request.CreatedAt >= createdFrom);
            }

            if (filter.CreatedTo.HasValue)
            {
                var createdTo = filter.CreatedTo.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(request => request.CreatedAt <= createdTo);
            }

            if (filter.OccupantId.HasValue)
            {
                query = query.Where(request => request.OccupantId == filter.OccupantId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var pageNumber = Math.Max(filter.PageNumber, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, 50);

            var items = await query
                .OrderByDescending(request => request.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public Task<MaintenanceRequest?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return _context.MaintenanceRequests
                .Include(request => request.Occupant)
                .Include(request => request.PropertyUnit)
                    .ThenInclude(unit => unit!.Property)
                .FirstOrDefaultAsync(request => request.Id == id && !request.IsDeleted, cancellationToken);
        }

        public Task<MaintenanceRequest?> GetByIdWithHistoryAsync(long id, CancellationToken cancellationToken)
        {
            return _context.MaintenanceRequests
                .Include(request => request.Occupant)
                .Include(request => request.PropertyUnit)
                    .ThenInclude(unit => unit!.Property)
                .Include(request => request.StatusHistories)
                .FirstOrDefaultAsync(request => request.Id == id && !request.IsDeleted, cancellationToken);
        }

        public Task<bool> OccupantExistsAsync(long occupantId, CancellationToken cancellationToken)
        {
            return _context.Occupants.AnyAsync(occupant => occupant.Id == occupantId && !occupant.IsDeleted, cancellationToken);
        }
        public async Task<Occupant> GetOrCreateOccupantByNameAsync(string fullName, CancellationToken cancellationToken)
        {
            var normalized = fullName.Trim();
            var existing = await _context.Occupants
                .Include(occupant => occupant.UserAccount)
                .FirstOrDefaultAsync(occupant => !occupant.IsDeleted && occupant.FullName.ToLower() == normalized.ToLower(), cancellationToken);

            if (existing is not null) return existing;

            var emailBase = new string(normalized.ToLowerInvariant().Where(char.IsLetter).ToArray());
            if (string.IsNullOrWhiteSpace(emailBase)) emailBase = "requester";
            var email = $"{emailBase}.{DateTime.UtcNow:yyyyMMddHHmmssfff}@local.pms";
            var now = DateTime.UtcNow;

            var user = new UserAccount
            {
                Email = email,
                PasswordHash = "AutoCreatedRequester",
                RoleType = RoleType.Occupant,
                AccountStatus = AccountStatus.Active,
                CreatedAt = now
            };

            await _context.UserAccounts.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var occupant = new Occupant
            {
                UserAccountId = user.Id,
                FullName = normalized,
                OccupantType = OccupantType.Owner,
                OccupantStatus = "Active",
                CreatedAt = now
            };

            await _context.Occupants.AddAsync(occupant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return occupant;
        }

        public Task<bool> UnitExistsAsync(long unitId, CancellationToken cancellationToken)
        {
            return _context.PropertyUnits.AnyAsync(unit => unit.Id == unitId && !unit.IsDeleted, cancellationToken);
        }
        public Task<PropertyUnit?> GetUnitByNumberAsync(string unitNumber, CancellationToken cancellationToken)
        {
            var normalized = unitNumber.Trim().ToUpper();
            return _context.PropertyUnits.FirstOrDefaultAsync(unit =>
                !unit.IsDeleted && unit.UnitNumber.ToUpper() == normalized, cancellationToken);
        }
        public async Task<PropertyUnit> GetOrCreateUnitByNumberAsync(string unitNumber, CancellationToken cancellationToken)
        {
            var normalized = unitNumber.Trim().ToUpperInvariant();
            var existing = await _context.PropertyUnits.FirstOrDefaultAsync(unit =>
                !unit.IsDeleted && unit.UnitNumber.ToUpper() == normalized, cancellationToken);

            if (existing is not null) return existing;

            var propertyId = await _context.Properties
                .Where(property => !property.IsDeleted)
                .OrderBy(property => property.Id)
                .Select(property => property.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (propertyId <= 0)
            {
                var property = new Property
                {
                    PropertyName = "Default Property",
                    PropertyType = "Residential",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Properties.AddAsync(property, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                propertyId = property.Id;
            }

            var parts = normalized.Split('-');
            var unit = new PropertyUnit
            {
                PropertyId = propertyId,
                UnitNumber = normalized,
                Block = parts[0],
                FloorLevel = parts[1],
                Status = "Occupied",
                UnitType = "Residential",
                CreatedAt = DateTime.UtcNow,
                CurrentOccupants = 0,
                MaxOccupants = 4
            };

            await _context.PropertyUnits.AddAsync(unit, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return unit;
        }

        public Task<bool> HasDuplicatePendingSubmissionAsync(long occupantId, long unitId, string title, string issueType, string description, CancellationToken cancellationToken)
        {
            var normalizedTitle = title.Trim().ToLower();
            var normalizedIssueType = issueType.Trim().ToLower();
            var normalizedDescription = description.Trim().ToLower();

            return _context.MaintenanceRequests.AnyAsync(request =>
                !request.IsDeleted &&
                request.OccupantId == occupantId &&
                request.UnitId == unitId &&
                request.Status == RequestStatus.Pending &&
                request.Title.ToLower() == normalizedTitle &&
                request.AssetType != null && request.AssetType.ToLower() == normalizedIssueType &&
                request.Description != null && request.Description.ToLower() == normalizedDescription,
                cancellationToken);
        }

        public async Task<string> GetNextRequestNumberAsync(int year, CancellationToken cancellationToken)
        {
            var prefix = $"REQ-{year}-";
            var lastRequestNumber = await _context.MaintenanceRequests
                .Where(request => request.RequestNumber.StartsWith(prefix))
                .OrderByDescending(request => request.RequestNumber)
                .Select(request => request.RequestNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var nextSequence = 1;
            if (!string.IsNullOrWhiteSpace(lastRequestNumber))
            {
                var lastSegment = lastRequestNumber.Split('-').LastOrDefault();
                if (int.TryParse(lastSegment, out var parsedSequence))
                {
                    nextSequence = parsedSequence + 1;
                }
            }

            return $"{prefix}{nextSequence:0000}";
        }

        public async Task<IReadOnlyList<MaintenanceRequestStatusHistory>> GetHistoryAsync(long requestId, CancellationToken cancellationToken)
        {
            return await _context.MaintenanceRequestStatusHistories
                .AsNoTracking()
                .Where(history => history.RequestId == requestId)
                .OrderByDescending(history => history.PerformedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MaintenanceRequesterResponse>> GetRequestersAsync(CancellationToken cancellationToken)
        {
            return await _context.Occupants
                .AsNoTracking()
                .Include(occupant => occupant.UserAccount)
                .Where(occupant => !occupant.IsDeleted && occupant.OccupantStatus != "Inactive")
                .OrderBy(occupant => occupant.FullName)
                .Select(occupant => new MaintenanceRequesterResponse
                {
                    OccupantID = occupant.Id,
                    FullName = occupant.FullName,
                    OccupantType = occupant.OccupantType.ToString(),
                    Email = occupant.UserAccount != null ? occupant.UserAccount.Email : null
                })
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken)
        {
            await _context.MaintenanceRequests.AddAsync(request, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private static bool TryParseStatus(string? value, out List<RequestStatus> statuses)
        {
            statuses = new List<RequestStatus>();
            if (string.IsNullOrWhiteSpace(value)) return false;
            
            var normalized = value.Replace(" ", "").Replace("_", "");
            if (Enum.TryParse<RequestStatus>(normalized, true, out var status) && Enum.IsDefined(typeof(RequestStatus), status))
            {
                statuses.Add(status);
                // For tenant UI compatibility: If filtering by Approved, also return Assigned
                if (status == RequestStatus.Approved)
                {
                    statuses.Add(RequestStatus.Assigned);
                }
                return true;
            }
            return false;
        }

        private static bool TryParsePriority(string? value, out PriorityLevel priority)
        {
            return Enum.TryParse(value, true, out priority) && Enum.IsDefined(typeof(PriorityLevel), priority);
        }
    }
}




