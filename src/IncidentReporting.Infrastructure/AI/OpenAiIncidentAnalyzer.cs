using IncidentReporting.Application.AI;
using IncidentReporting.Application.AI.Dtos;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentReporting.Infrastructure.AI;

public class OpenAiIncidentAnalyzer : IAiIncidentAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;
    private readonly ILogger<OpenAiIncidentAnalyzer> _logger;

    public OpenAiIncidentAnalyzer(HttpClient httpClient, IOptions<AiSettings> settings, ILogger<OpenAiIncidentAnalyzer> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AiAnalysisResultDto> AnalyzeIncidentAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("OpenAI ApiKey is not configured. Falling back to basic mock response.");
            return GetFallbackResult();
        }

        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var systemPrompt = @"You are an AI Incident Analyzer. Analyze the title and description of the incident and output a valid JSON object matching the following structure. Do NOT wrap the JSON in markdown code blocks or backticks.
{
  ""summary"": ""A concise 1-2 sentence summary of the issue."",
  ""suggestedCategory"": 1,
  ""suggestedPriority"": 2,
  ""sentiment"": ""Negative/Neutral/Positive"",
  ""keyPhrases"": [""keyphrase1"", ""keyphrase2""],
  ""isUrgent"": false
}

Note:
For suggestedCategory, use the enum integer value:
1 = Infrastructure, 2 = Network, 3 = Security, 4 = Application, 5 = Database, 6 = Access, 99 = Other

For suggestedPriority, use the enum integer value:
1 = Low, 2 = Medium, 3 = High, 4 = Critical";

            var userPrompt = $"Title: {title}\nDescription: {description}";

            var requestBody = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1
            };

            requestMessage.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenAI API call failed. Status: {StatusCode}. Response: {Response}", response.StatusCode, errorContent);
                return GetFallbackResult();
            }

            var openAiResponse = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cancellationToken);
            var content = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAI returned an empty content response.");
                return GetFallbackResult();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsedResult = JsonSerializer.Deserialize<OpenAiAnalysisOutput>(content, options);

            if (parsedResult != null)
            {
                return new AiAnalysisResultDto
                {
                    Summary = parsedResult.Summary,
                    SuggestedCategory = Enum.IsDefined(typeof(IncidentCategory), parsedResult.SuggestedCategory) ? (IncidentCategory)parsedResult.SuggestedCategory : IncidentCategory.Other,
                    SuggestedPriority = Enum.IsDefined(typeof(IncidentPriority), parsedResult.SuggestedPriority) ? (IncidentPriority)parsedResult.SuggestedPriority : IncidentPriority.Low,
                    Sentiment = parsedResult.Sentiment,
                    KeyPhrases = parsedResult.KeyPhrases ?? new List<string>(),
                    IsUrgent = parsedResult.IsUrgent
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI endpoint for incident analysis.");
        }

        return GetFallbackResult();
    }

    private static AiAnalysisResultDto GetFallbackResult()
    {
        return new AiAnalysisResultDto
        {
            Summary = "Fallback Summary: The AI service could not process the request.",
            SuggestedCategory = IncidentCategory.Other,
            SuggestedPriority = IncidentPriority.Low,
            Sentiment = "Neutral",
            KeyPhrases = new List<string> { "fallback" },
            IsUrgent = false
        };
    }

    private class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAiAnalysisOutput
    {
        public string Summary { get; set; } = string.Empty;
        public int SuggestedCategory { get; set; }
        public int SuggestedPriority { get; set; }
        public string Sentiment { get; set; } = string.Empty;
        public List<string>? KeyPhrases { get; set; }
        public bool IsUrgent { get; set; }
    }
}
