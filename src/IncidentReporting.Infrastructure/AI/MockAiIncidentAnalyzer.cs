using IncidentReporting.Application.AI;
using IncidentReporting.Application.AI.Dtos;
using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Infrastructure.AI;

public class MockAiIncidentAnalyzer : IAiIncidentAnalyzer
{
    public Task<AiAnalysisResultDto> AnalyzeIncidentAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        var textToAnalyze = $"{title} {description}".ToLowerInvariant();

        var suggestedCategory = IncidentCategory.Other;
        var suggestedPriority = IncidentPriority.Low;
        var isUrgent = false;
        var sentiment = "Neutral";
        var keyPhrases = new List<string>();

        // Basic classification rules based on keywords
        if (textToAnalyze.Contains("database") || textToAnalyze.Contains("sql") || textToAnalyze.Contains("db"))
        {
            suggestedCategory = IncidentCategory.Database;
            keyPhrases.Add("database");
        }
        else if (textToAnalyze.Contains("network") || textToAnalyze.Contains("dns") || textToAnalyze.Contains("router"))
        {
            suggestedCategory = IncidentCategory.Network;
            keyPhrases.Add("network");
        }
        else if (textToAnalyze.Contains("security") || textToAnalyze.Contains("hack") || textToAnalyze.Contains("auth") || textToAnalyze.Contains("breach") || textToAnalyze.Contains("password"))
        {
            suggestedCategory = IncidentCategory.Security;
            suggestedPriority = IncidentPriority.Critical;
            isUrgent = true;
            sentiment = "Negative";
            keyPhrases.Add("security");
        }
        else if (textToAnalyze.Contains("app") || textToAnalyze.Contains("ui") || textToAnalyze.Contains("frontend") || textToAnalyze.Contains("bug"))
        {
            suggestedCategory = IncidentCategory.Application;
            keyPhrases.Add("application");
        }
        else if (textToAnalyze.Contains("server") || textToAnalyze.Contains("infrastructure") || textToAnalyze.Contains("cloud"))
        {
            suggestedCategory = IncidentCategory.Infrastructure;
            keyPhrases.Add("infrastructure");
        }

        // Priority rules
        if (textToAnalyze.Contains("down") || textToAnalyze.Contains("outage") || textToAnalyze.Contains("crash") || textToAnalyze.Contains("urgent"))
        {
            suggestedPriority = suggestedPriority < IncidentPriority.High ? IncidentPriority.High : suggestedPriority;
            isUrgent = true;
            sentiment = "Negative";
            keyPhrases.Add("outage");
        }
        else if (textToAnalyze.Contains("slow") || textToAnalyze.Contains("warning"))
        {
            suggestedPriority = suggestedPriority < IncidentPriority.Medium ? IncidentPriority.Medium : suggestedPriority;
            keyPhrases.Add("performance issues");
        }

        if (keyPhrases.Count == 0)
        {
            keyPhrases.Add("general incident");
        }

        var result = new AiAnalysisResultDto
        {
            Summary = $"Mocked AI Summary: The incident relates to {suggestedCategory.ToString().ToLower()} and has been flagged with {suggestedPriority.ToString().ToLower()} priority.",
            SuggestedCategory = suggestedCategory,
            SuggestedPriority = suggestedPriority,
            Sentiment = sentiment,
            KeyPhrases = keyPhrases,
            IsUrgent = isUrgent
        };

        return Task.FromResult(result);
    }
}
