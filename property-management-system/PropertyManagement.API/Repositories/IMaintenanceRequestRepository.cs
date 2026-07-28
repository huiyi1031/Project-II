using PropertyManagement.API.Models.DTOs.MaintenanceRequests;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Repositories
{
    public interface IMaintenanceRequestRepository
    {
        Task<(IReadOnlyList<MaintenanceRequest> Items, int TotalCount)> GetPagedAsync(MaintenanceRequestFilterRequest filter, CancellationToken cancellationToken);
        Task<MaintenanceRequest?> GetByIdAsync(long id, CancellationToken cancellationToken);
        Task<MaintenanceRequest?> GetByIdWithHistoryAsync(long id, CancellationToken cancellationToken);
        Task<bool> OccupantExistsAsync(long occupantId, CancellationToken cancellationToken);
        Task<Occupant> GetOrCreateOccupantByNameAsync(string fullName, CancellationToken cancellationToken);
        Task<bool> UnitExistsAsync(long unitId, CancellationToken cancellationToken);
        Task<PropertyUnit?> GetUnitByNumberAsync(string unitNumber, CancellationToken cancellationToken);
        Task<PropertyUnit> GetOrCreateUnitByNumberAsync(string unitNumber, CancellationToken cancellationToken);
        Task<bool> HasDuplicatePendingSubmissionAsync(long occupantId, long unitId, string title, string issueType, string description, CancellationToken cancellationToken);
        Task<string> GetNextRequestNumberAsync(int year, CancellationToken cancellationToken);
        Task<IReadOnlyList<MaintenanceRequestStatusHistory>> GetHistoryAsync(long requestId, CancellationToken cancellationToken);
        Task<IReadOnlyList<MaintenanceRequesterResponse>> GetRequestersAsync(CancellationToken cancellationToken);
        Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}



