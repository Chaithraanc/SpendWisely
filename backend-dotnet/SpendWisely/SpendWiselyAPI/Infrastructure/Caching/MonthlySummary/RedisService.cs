using StackExchange.Redis;

namespace SpendWiselyAPI.Infrastructure.Caching.MonthlySummary
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        private static string UserTotalKey(Guid userId)
            => $"user:{userId}:total";

        private static string UserCategoryKey(Guid userId, Guid categoryId)
            => $"user:{userId}:category:{categoryId}";

        private static string monthly_summary_generated_event_key(int year, int month)
            => $"monthly_summary_generated:{year}:{month}";

        private static string monthly_AIInsights_generated_event_key(int year, int month)
       => $"monthly_AIInsights_generated:{year}:{month}";
        // -----------------------------
        // SETTERS
        // -----------------------------
        public async Task SetUserTotalAsync(Guid userId, decimal total)
        {
            await _db.StringSetAsync(UserTotalKey(userId), total.ToString());
        }

        public async Task SetUserCategoryTotalAsync(Guid userId, Guid categoryId, decimal total)
        {
            await _db.StringSetAsync(UserCategoryKey(userId, categoryId), total.ToString());
        }

        public async Task SetMonthlySummaryGeneratedAsync(int year, int month)
        {
            await _db.StringSetAsync(monthly_summary_generated_event_key(year, month), "true", TimeSpan.FromDays(7));
        }
            public async Task SetMonthlyAIInsightsGeneratedAsync(int year, int month)
            {
                await _db.StringSetAsync(monthly_AIInsights_generated_event_key(year, month), "true", TimeSpan.FromDays(7));
        }

        // -----------------------------
        // GETTERS
        // -----------------------------
        public async Task<decimal?> GetUserTotalAsync(Guid userId)
        {
            var value = await _db.StringGetAsync(UserTotalKey(userId));
            return value.HasValue ? (decimal)value : null;
        }

        public async Task<decimal?> GetUserCategoryTotalAsync(Guid userId, Guid categoryId)
        {
            var value = await _db.StringGetAsync(UserCategoryKey(userId, categoryId));
            return value.HasValue ? (decimal)value : null;
        }

            public async Task<bool> HasMonthlySummaryGeneratedAsync(int year, int month)
            {
                return await _db.KeyExistsAsync(monthly_summary_generated_event_key(year, month));
            }

        public async Task<bool> HasMonthlyAIInsightsGeneratedAsync(int year, int month)
        {
            return await _db.KeyExistsAsync(monthly_AIInsights_generated_event_key(year, month));
        }
        // -----------------------------
        // RESET LOGIC
        // -----------------------------
        public async Task ResetUserAsync(Guid userId)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            foreach (var key in server.Keys(pattern: $"user:{userId}:*"))
            {
                await _db.StringSetAsync(key, 0);
            }
        }

        public async Task ResetAllUsersAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            foreach (var key in server.Keys(pattern: "user:*"))
            {
                await _db.StringSetAsync(key, 0);
            }
        }
    }
}
