using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IncidentReporting.Infrastructure.Repositories;

public class IncidentAttachmentRepository : IIncidentAttachmentRepository
{
    private readonly AppDbContext _context;

    public IncidentAttachmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IncidentAttachment> AddAsync(
        IncidentAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        await _context.IncidentAttachments.AddAsync(attachment, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return attachment;
    }

    public async Task<IncidentAttachment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.IncidentAttachments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<IncidentAttachment>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.IncidentAttachments
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
