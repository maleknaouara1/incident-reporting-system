using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Users;
using IncidentReporting.Application.Users.Dtos;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReporting.UnitTests;

public class UserManagementServiceTests
{
    private class ListUserRepository : IUserRepository
    {
        public List<ApplicationUser> Users { get; } = new();

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

        public Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

        public Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.ToList());

        public Task<bool> AnyWithRoleAsync(UserRole role, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.Any(u => u.Role == role));
    }

    private static ApplicationUser User(string name, UserRole role, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        FullName = name,
        Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.com",
        PasswordHash = "secret-hash",
        Role = role,
        IsActive = isActive
    };

    [Fact]
    public async Task GetDirectoryAsync_ReturnsEveryUserSortedByName()
    {
        var repo = new ListUserRepository();
        repo.Users.AddRange(new[]
        {
            User("Zoe Admin", UserRole.Admin),
            User("adam Reporter", UserRole.Reporter),
            User("Eric Engineer", UserRole.Engineer, isActive: false)
        });

        var response = await new UserManagementService(repo).GetDirectoryAsync();

        Assert.True(response.Success);
        Assert.Equal(new[] { "adam Reporter", "Eric Engineer", "Zoe Admin" }, response.Data!.Select(u => u.FullName));
        var eric = response.Data!.Single(u => u.FullName == "Eric Engineer");
        Assert.Equal(UserRole.Engineer, eric.Role);
        Assert.False(eric.IsActive);
    }

    [Fact]
    public void DirectoryEntries_DoNotCarryEmailsOrAccountDetails()
    {
        var properties = typeof(UserDirectoryEntryDto).GetProperties().Select(p => p.Name).ToArray();

        Assert.Equal(new[] { "Id", "FullName", "Role", "IsActive" }, properties);
    }
}
