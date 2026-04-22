using StackExchange.Redis;
using System.Text.Json;

namespace SpendWiselyAPI.Infrastructure.Caching
{
  
    public class RedisCache : IRedisCache
    {
        private readonly IDatabase _db;

        public RedisCache(IConnectionMultiplexer connectionMultiplexer)
        {
            _db = connectionMultiplexer.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return default;
            }
            return System.Text.Json.JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, (StackExchange.Redis.Expiration)expiry);
        }
    }
}


