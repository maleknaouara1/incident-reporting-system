using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IncidentReporting.Infrastructure.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly AppDbContext _context;

    public IncidentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Incident> AddAsync(
        Incident incident,
        CancellationToken cancellationToken = default)
    {
        await _context.Incidents.AddAsync(incident, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return incident;
    }

    public async Task<Incident?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Incidents
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Incident>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Incidents
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
 public async Task<Incident> UpdateAsync(
    Incident incident,
    CancellationToken cancellationToken = default)
{
    _context.Incidents.Update(incident);
    await _context.SaveChangesAsync(cancellationToken);
    return incident;
}
public async Task<List<IncidentHistory>> GetHistoryByIncidentIdAsync(
    Guid incidentId,
    CancellationToken cancellationToken = default)
{
    return await _context.IncidentHistories
        .Where(x => x.IncidentId == incidentId)
        .OrderBy(x => x.CreatedAtUtc)
        .ToListAsync(cancellationToken);
}
}