using IncidentReporting.Application.AI;
using IncidentReporting.Application.AI.Dtos;
using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Application.Incidents.Services;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReporting.UnitTests;

public class EmailIncidentServiceTests
{
    private class CountingAiIncidentAnalyzer : IAiIncidentAnalyzer
    {
        public int CallCount { get; private set; }

        public Task<AiAnalysisResultDto> AnalyzeIncidentAsync(string title, string description, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new AiAnalysisResultDto
            {
                Summary = "Should not be used for the email intake path.",
                SuggestedCategory = IncidentCategory.Other,
                SuggestedPriority = IncidentPriority.Low
            });
        }
    }

    private class InMemoryIncidentRepository : IIncidentRepository
    {
        private readonly List<Incident> _incidents = new();
        public int AddCount { get; private set; }

        public Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            AddCount++;
            _incidents.Add(incident);
            return Task.FromResult(incident);
        }

        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_incidents.FirstOrDefault(x => x.Id == id));
        }

        public Task<List<Incident>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_incidents.ToList());
        }

        public Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(GetIncidentsRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((_incidents.ToList(), _incidents.Count));
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
            return Task.FromResult(_incidents.FirstOrDefault(x => x.SourceMessageId == sourceMessageId));
        }
    }

    private class StubUserRepository : IUserRepository
    {
        public ApplicationUser? SystemUser { get; set; }

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ApplicationUser?>(null);
        }

        public Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SystemUser is not null && SystemUser.Id == id ? SystemUser : null);
        }

        public Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user);
        }

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SystemUser is not null ? new List<ApplicationUser> { SystemUser } : new List<ApplicationUser>());
        }

        public Task<bool> AnyWithRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SystemUser is not null && SystemUser.Role == role);
        }
    }

    private class StubCurrentUserService : ICurrentUserService
    {
        public Guid UserId => Guid.NewGuid();
        public string Email => "manual@test.com";
        public UserRole Role => UserRole.Reporter;
        public bool IsAuthenticated => true;
    }

    private class NoOpN8nService : IN8nService
    {
        public Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyIncidentAssignedAsync(IncidentDto incident, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyIncidentStatusChangedAsync(IncidentDto incident, IncidentStatus oldStatus, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static (EmailIncidentService Service, CountingAiIncidentAnalyzer Analyzer, InMemoryIncidentRepository Repository) CreateService()
    {
        var systemUserId = Guid.NewGuid();
        var analyzer = new CountingAiIncidentAnalyzer();
        var repository = new InMemoryIncidentRepository();
        var users = new StubUserRepository
        {
            SystemUser = new ApplicationUser { Id = systemUserId, Email = "system@intake.local", FullName = "System", IsActive = true }
        };
        var incidentService = new IncidentService(repository, new StubCurrentUserService(), analyzer, new NoOpN8nService(), new StubUserRepository());
        var settings = Options.Create(new EmailIntakeSettings
        {
            IsEnabled = true,
            ApiKey = "test-key",
            SystemUserId = systemUserId,
            DefaultCategory = IncidentCategory.Other,
            DefaultPriority = IncidentPriority.Medium
        });

        var service = new EmailIncidentService(incidentService, repository, users, settings, NullLogger<EmailIncidentService>.Instance);
        return (service, analyzer, repository);
    }

    private static EmailIncidentRequest ValidRequest(string messageId = "msg-1") => new()
    {
        From = "reporter@example.com",
        Subject = "Production database unavailable",
        Body = "All users cannot access the application.",
        ReceivedAtUtc = DateTime.UtcNow,
        MessageId = messageId,
        AiAnalysis = new AiAnalysisResultDto
        {
            IsIncident = true,
            Summary = "Production database outage affecting all users.",
            SuggestedCategory = IncidentCategory.Database,
            SuggestedPriority = IncidentPriority.Critical,
            SuggestedResolution = "Check database availability and service logs.",
            ConfidenceScore = 0.96,
            Sentiment = "Negative",
            KeyPhrases = new List<string> { "database unavailable", "all users affected" }
        }
    };

    [Fact]
    public async Task CreateFromEmailAsync_WithPrecomputedAnalysis_SkipsAnalyzerAndUsesSuppliedClassification()
    {
        var (service, analyzer, repository) = CreateService();

        var response = await service.CreateFromEmailAsync(ValidRequest());

        Assert.True(response.Success);
        Assert.Equal(0, analyzer.CallCount);
        Assert.Equal(1, repository.AddCount);
        Assert.Equal(IncidentCategory.Database, response.Data!.Category);
        Assert.Equal(IncidentPriority.Critical, response.Data.Priority);
        Assert.Equal(0.96, response.Data.AiConfidenceScore);
        Assert.Equal("Check database availability and service logs.", response.Data.AiSuggestedResolution);
        Assert.Equal(IncidentSource.Email, response.Data.Source);
    }

    [Fact]
    public async Task CreateFromEmailAsync_WhenIsIncidentFalse_CreatesNothingAndDoesNotCallAnalyzer()
    {
        var (service, analyzer, repository) = CreateService();
        var request = ValidRequest();
        request.AiAnalysis.IsIncident = false;

        var response = await service.CreateFromEmailAsync(request);

        Assert.False(response.Success);
        Assert.Equal(0, analyzer.CallCount);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task CreateFromEmailAsync_WithDuplicateMessageId_ReturnsExistingIncidentAndCreatesNothingNew()
    {
        var (service, analyzer, repository) = CreateService();

        var first = await service.CreateFromEmailAsync(ValidRequest("dup-1"));
        Assert.True(first.Success);
        Assert.Equal(1, repository.AddCount);

        var second = await service.CreateFromEmailAsync(ValidRequest("dup-1"));

        Assert.True(second.Success);
        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Equal(1, repository.AddCount);
        Assert.Equal(0, analyzer.CallCount);
    }
}
