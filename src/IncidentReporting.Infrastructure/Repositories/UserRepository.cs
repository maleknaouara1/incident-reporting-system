using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Domain.Entities;
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
}