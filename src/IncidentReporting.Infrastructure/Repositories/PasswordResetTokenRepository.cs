using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IncidentReporting.Infrastructure.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _context;

    public PasswordResetTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task UpdateAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Update(token);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
