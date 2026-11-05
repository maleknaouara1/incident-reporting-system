using IncidentReporting.Application.AI;
using IncidentReporting.Application.AI.Dtos;
using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Application.Incidents.Services;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReporting.UnitTests;

public class IncidentServiceTests
{
    private class MockCurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; init; } = Guid.NewGuid();
        public string Email => "test@test.com";
        public UserRole Role { get; init; } = UserRole.Reporter;
        public bool IsAuthenticated => true;
    }

    private class MockUserRepository : IUserRepository
    {
        public List<ApplicationUser> Users { get; } = new();

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.Find(u => u.Email == email));

        public Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.Find(u => u.Id == id));

        public Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Users);

        public Task<bool> AnyWithRoleAsync(UserRole role, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.Exists(u => u.Role == role));
    }

    private class MockIncidentRepository : IIncidentRepository
    {
        // Returned by GetByIdAsync when its id matches
        public Incident? Stored { get; set; }

        public Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(incident);
        }

        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored?.Id == id ? Stored : null);
        }

        public Task<List<Incident>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<Incident>());
        }

        public Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(GetIncidentsRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((new List<Incident>(), 0));
        }

        public Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(incident);
        }

        public Task<List<IncidentHistory>> GetHistoryByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<IncidentHistory>());
        }

        public Task<Incident?> GetBySourceMessageIdAsync(string sourceMessageId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Incident?>(null);
        }
    }

    private class MockN8nService : IN8nService
    {
        public List<IncidentDto> NotifiedIncidents { get; } = new();

        public Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default)
        {
            NotifiedIncidents.Add(incident);
            return Task.CompletedTask;
        }

        public Task NotifyIncidentAssignedAsync(IncidentDto incident, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyIncidentStatusChangedAsync(IncidentDto incident, IncidentStatus oldStatus, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private class MockAiIncidentAnalyzer : IAiIncidentAnalyzer
    {
        public int CallCount { get; private set; }

        public Task<AiAnalysisResultDto> AnalyzeIncidentAsync(string title, string description, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new AiAnalysisResultDto
            {
                Summary = "Mocked AI Summary: The incident relates to database and has been flagged with high priority.",
                SuggestedCategory = IncidentCategory.Database,
                SuggestedPriority = IncidentPriority.High,
                Sentiment = "Negative",
                KeyPhrases = new List<string> { "database" },
                IsUrgent = true
            });
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldPopulateAiFieldsAndNotifyN8n_UsingMockAiIncidentAnalyzer()
    {
        // Arrange
        var mockRepo = new MockIncidentRepository();
        var mockUserService = new MockCurrentUserService();
        var mockAiAnalyzer = new MockAiIncidentAnalyzer();
        var mockN8nService = new MockN8nService();

        var service = new IncidentService(mockRepo, mockUserService, mockAiAnalyzer, mockN8nService, new MockUserRepository());

        var request = new CreateIncidentRequest
        {
            Title = "Database SQL error detected",
            Description = "The database connection timed out during a query.",
            Category = IncidentCategory.Other,
            Priority = IncidentPriority.Low
        };

        // Act
        var response = await service.CreateAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("Database SQL error detected", response.Data.Title);
        
        // AI analysis fields assertion
        Assert.NotNull(response.Data.AiSummary);
        Assert.Contains("database", response.Data.AiSummary.ToLowerInvariant());
        Assert.NotNull(response.Data.AiSuggestedResolution);
        Assert.Contains("Database", response.Data.AiSuggestedResolution);
        Assert.Equal(0.95, response.Data.AiConfidenceScore);

        // N8n notification assertion
        Assert.Single(mockN8nService.NotifiedIncidents);
        Assert.Equal(response.Data.Id, mockN8nService.NotifiedIncidents[0].Id);

        // Manual creation must still call the backend AI analyzer exactly once
        Assert.Equal(1, mockAiAnalyzer.CallCount);
    }

    [Fact]
    public async Task CreateAsync_WithPrecomputedAiAnalysis_DoesNotCallAiIncidentAnalyzer()
    {
        // Arrange
        var mockRepo = new MockIncidentRepository();
        var mockUserService = new MockCurrentUserService();
        var mockAiAnalyzer = new MockAiIncidentAnalyzer();
        var mockN8nService = new MockN8nService();

        var service = new IncidentService(mockRepo, mockUserService, mockAiAnalyzer, mockN8nService, new MockUserRepository());

        var request = new CreateIncidentRequest
        {
            Title = "Production database unavailable",
            Description = "All users cannot access the application.",
            Category = IncidentCategory.Database,
            Priority = IncidentPriority.Critical
        };

        var precomputed = new AiAnalysisResultDto
        {
            IsIncident = true,
            Summary = "Production database outage affecting all users.",
            SuggestedCategory = IncidentCategory.Database,
            SuggestedPriority = IncidentPriority.Critical,
            SuggestedResolution = "Check database availability and service logs.",
            ConfidenceScore = 0.96,
            Sentiment = "Negative",
            KeyPhrases = new List<string> { "database unavailable", "all users affected" }
        };

        var context = new IncidentCreationContext(Guid.NewGuid(), IncidentSource.Email, "msg-123", precomputed);

        // Act
        var response = await service.CreateAsync(request, context);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(0, mockAiAnalyzer.CallCount);
        Assert.Equal("Check database availability and service logs.", response.Data!.AiSuggestedResolution);
        Assert.Equal(0.96, response.Data.AiConfidenceScore);
        Assert.Equal(IncidentSource.Email, response.Data.Source);
    }

    private static Incident NewIncident(IncidentStatus status = IncidentStatus.New) => new()
    {
        Id = Guid.NewGuid(),
        Title = "VPN down",
        Description = "Remote staff cannot connect.",
        Status = status,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static ApplicationUser NewUser(UserRole role, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        FullName = $"{role} User",
        Email = $"{Guid.NewGuid()}@test.com",
        Role = role,
        IsActive = isActive
    };

    private static (IncidentService Service, MockIncidentRepository Repo, MockUserRepository Users) CreateStaffService(
        Incident incident, Guid? currentUserId = null)
    {
        var repo = new MockIncidentRepository { Stored = incident };
        var users = new MockUserRepository();
        var currentUser = new MockCurrentUserService { UserId = currentUserId ?? Guid.NewGuid(), Role = UserRole.Engineer };
        var service = new IncidentService(repo, currentUser, new MockAiIncidentAnalyzer(), new MockN8nService(), users);
        return (service, repo, users);
    }

    [Fact]
    public async Task AddCommentAsync_UsesSignedInUserAsAuthor()
    {
        var currentUserId = Guid.NewGuid();
        var incident = NewIncident(IncidentStatus.InProgress);
        var (service, _, _) = CreateStaffService(incident, currentUserId);

        var response = await service.AddCommentAsync(incident.Id, new AddIncidentCommentRequest { Message = "  Checking logs  " });

        Assert.True(response.Success);
        var comment = Assert.Single(response.Data!.Comments);
        Assert.Equal(currentUserId, comment.AuthorUserId);
        Assert.Equal("Checking logs", comment.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddCommentAsync_RejectsEmptyMessage(string message)
    {
        var incident = NewIncident();
        var (service, _, _) = CreateStaffService(incident);

        var response = await service.AddCommentAsync(incident.Id, new AddIncidentCommentRequest { Message = message });

        Assert.False(response.Success);
        Assert.Empty(incident.Comments);
    }

    [Fact]
    public async Task AssignAsync_ToActiveEngineer_MovesNewIncidentToAssigned()
    {
        var incident = NewIncident();
        var (service, _, users) = CreateStaffService(incident);
        var engineer = NewUser(UserRole.Engineer);
        users.Users.Add(engineer);

        var response = await service.AssignAsync(incident.Id, new AssignIncidentRequest { AssignedToUserId = engineer.Id });

        Assert.True(response.Success);
        Assert.Equal(engineer.Id, response.Data!.AssignedToUserId);
        Assert.Equal(IncidentStatus.Assigned, response.Data.Status);
    }

    [Fact]
    public async Task AssignAsync_Reassigning_KeepsCurrentStatus()
    {
        var incident = NewIncident(IncidentStatus.InProgress);
        var (service, _, users) = CreateStaffService(incident);
        var engineer = NewUser(UserRole.Engineer);
        users.Users.Add(engineer);

        var response = await service.AssignAsync(incident.Id, new AssignIncidentRequest { AssignedToUserId = engineer.Id });

        Assert.True(response.Success);
        Assert.Equal(IncidentStatus.InProgress, response.Data!.Status);
    }

    [Theory]
    [InlineData(UserRole.Reporter, true)]
    [InlineData(UserRole.Engineer, false)]
    public async Task AssignAsync_RejectsReportersAndInactiveUsers(UserRole role, bool isActive)
    {
        var incident = NewIncident();
        var (service, _, users) = CreateStaffService(incident);
        var user = NewUser(role, isActive);
        users.Users.Add(user);

        var response = await service.AssignAsync(incident.Id, new AssignIncidentRequest { AssignedToUserId = user.Id });

        Assert.False(response.Success);
        Assert.Null(incident.AssignedToUserId);
        Assert.Equal(IncidentStatus.New, incident.Status);
    }

    [Fact]
    public async Task AssignAsync_RejectsUnknownUser()
    {
        var incident = NewIncident();
        var (service, _, _) = CreateStaffService(incident);

        var response = await service.AssignAsync(incident.Id, new AssignIncidentRequest { AssignedToUserId = Guid.NewGuid() });

        Assert.False(response.Success);
        Assert.Null(incident.AssignedToUserId);
    }
}
