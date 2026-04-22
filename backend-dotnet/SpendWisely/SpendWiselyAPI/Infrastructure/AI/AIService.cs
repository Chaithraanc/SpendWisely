using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using RabbitMQ.Stream.Client;
using SpendWiselyAPI.Application.DTOs.AIInsights;
using SpendWiselyAPI.Application.DTOs.Budget;
using SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary;
using SpendWiselyAPI.Infrastructure.AI;
using SpendWiselyAPI.Workers.DashboardSummaryGenerator;
using StackExchange.Redis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;


public class AIService : IAIService
{
    
    private readonly ILogger<AIService> _logger;
    private readonly OpenAIClient _client;

    public AIService(
     ILogger<AIService> logger,
     IOptions<OpenAISettings> settings)
    {
        _logger = logger;

        var apiKey = settings.Value.ApiKey;

        _client = new OpenAIClient(apiKey);
    }

    public async Task<string> CategorizeExpenseAsync(string description)
    {
        var prompt = $@"
Categorize this expense into one category:
Food, Travel, Shopping, Groceries, Entertainment, Bills, Health, Education, Other.

Description: {description}

Return only category name.
";
        var chatClient = _client.GetChatClient("gpt-4o-mini");
        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
        new SystemChatMessage("You categorize expenses."),
        new UserChatMessage(prompt)
            }
            
        );

        var category = response.Value.Content[0].Text?.Trim();

        return string.IsNullOrWhiteSpace(category) ? "Other" : category;
    }

   

    public async Task<string> GenerateMonthlyInsightsBatchAsync(
    AIInsightsBatchInput batch,
    CancellationToken cancellationToken = default)
    {
        var prompt = BuildBatchPrompt(batch.Users);

        try
        {
            var chatClient = _client.GetChatClient("gpt-4o-mini");

            var response = await chatClient.CompleteChatAsync(
                new List<ChatMessage>
                {
                new SystemChatMessage("You are a financial AI that returns strict JSON only."),
                new UserChatMessage(prompt)
                },
                cancellationToken: cancellationToken
            );

            var json = response.Value.Content[0].Text;

            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("AI returned empty JSON");

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate batch AI insights");
            throw;
        }
    }
    private string BuildBatchPrompt(IReadOnlyList<AIInsightsInput> users)
    {
        return $@"
Generate monthly financial insights for multiple users.

### Users Data
{JsonSerializer.Serialize(users)}

### Output Requirements
Return ONLY valid JSON.

JSON must be an array where each item contains:
- userId: string
- summary: string
- recommendations: array of strings
- spendingSpikes: array of objects
- anomalies: array of objects
- forecast: object

No markdown. No explanation. No text outside JSON.
";
    }





}


