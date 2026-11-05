using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using IncidentReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IncidentReporting.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                x => x.Email == email,
                cancellationToken);
    }

    public async Task<ApplicationUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<ApplicationUser> AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(
            user,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        return user;
    }

    public async Task UpdateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ApplicationUser>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyWithRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(x => x.Role == role, cancellationToken);
    }
}