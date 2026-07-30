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
        public Guid UserId => Guid.NewGuid();
        public string Email => "test@test.com";
        public UserRole Role => UserRole.Reporter;
        public bool IsAuthenticated => true;
    }

    private class MockIncidentRepository : IIncidentRepository
    {
        public Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(incident);
        }

        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Incident?>(null);
        }

        public Task<List<Incident>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<Incident>());
        }

        public Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(incident);
        }

        public Task<List<IncidentHistory>> GetHistoryByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<IncidentHistory>());
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
    }

    private class MockAiIncidentAnalyzer : IAiIncidentAnalyzer
    {
        public Task<AiAnalysisResultDto> AnalyzeIncidentAsync(string title, string description, CancellationToken cancellationToken = default)
        {
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

        var service = new IncidentService(mockRepo, mockUserService, mockAiAnalyzer, mockN8nService);

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
    }
}
