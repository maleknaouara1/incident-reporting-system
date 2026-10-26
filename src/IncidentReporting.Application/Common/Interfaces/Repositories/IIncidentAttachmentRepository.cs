using IncidentReporting.Domain.Entities;

namespace IncidentReporting.Application.Common.Interfaces.Repositories;

public interface IIncidentAttachmentRepository
{
    Task<IncidentAttachment> AddAsync(
        IncidentAttachment attachment,
        CancellationToken cancellationToken = default);

    Task<IncidentAttachment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<IncidentAttachment>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}
