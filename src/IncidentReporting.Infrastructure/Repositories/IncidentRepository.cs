using System.Linq.Expressions;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Exceptions;
using IncidentReporting.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IncidentReporting.Infrastructure.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlUniqueIndexViolation = 2601;

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

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSourceMessageIdUniqueViolation(ex))
        {
            throw new DuplicateIncidentSourceMessageIdException(incident.SourceMessageId!);
        }

        return incident;
    }

    private static bool IsSourceMessageIdUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx &&
               (sqlEx.Number == SqlUniqueConstraintViolation || sqlEx.Number == SqlUniqueIndexViolation);
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

    public async Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(
        GetIncidentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Incidents.AsQueryable();

        if (request.Status is not null)
            query = query.Where(x => x.Status == request.Status);

        if (request.Priority is not null)
            query = query.Where(x => x.Priority == request.Priority);

        if (request.Category is not null)
            query = query.Where(x => x.Category == request.Category);

        if (request.AssignedToUserId is not null)
            query = query.Where(x => x.AssignedToUserId == request.AssignedToUserId);

        if (request.CreatedByUserId is not null)
            query = query.Where(x => x.CreatedByUserId == request.CreatedByUserId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Description.Contains(term));
        }

        query = ApplySort(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<Incident> ApplySort(
        IQueryable<Incident> query,
        string? sortBy,
        bool descending)
    {
        Expression<Func<Incident, object>> keySelector = sortBy?.Trim().ToLowerInvariant() switch
        {
            "priority" => x => x.Priority,
            "status" => x => x.Status,
            "title" => x => x.Title,
            "updatedatutc" => x => x.UpdatedAtUtc!,
            _ => x => x.CreatedAtUtc
        };

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

 public async Task<Incident> UpdateAsync(
    Incident incident,
    CancellationToken cancellationToken = default)
{
    // The incident here is normally already tracked (it came from GetByIdAsync on this
    // same DbContext). Calling Update() unconditionally would walk the whole graph and
    // mark newly-added children (Comments/Histories/Attachments) as Modified instead of
    // Added, because their Ids are assigned client-side (not DB-generated) -- EF can't
    // tell them apart from existing rows, and the resulting UPDATE affects 0 rows and
    // throws DbUpdateConcurrencyException. Only attach+mark-modified when the caller
    // passed in a genuinely detached entity.
    if (_context.Entry(incident).State == EntityState.Detached)
    {
        _context.Incidents.Update(incident);
    }

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

public async Task<Incident?> GetBySourceMessageIdAsync(
    string sourceMessageId,
    CancellationToken cancellationToken = default)
{
    return await _context.Incidents
        .FirstOrDefaultAsync(x => x.SourceMessageId == sourceMessageId, cancellationToken);
}
}