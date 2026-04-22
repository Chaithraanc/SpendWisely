using SpendWiselyAPI.Application.DTOs.AIInsights;

namespace SpendWiselyAPI.Infrastructure.AI
{
    public sealed class AIInsightsBatchInput
    {
        public IReadOnlyList<AIInsightsInput> Users { get; init; }
    }
}
