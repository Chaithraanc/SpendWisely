namespace SpendWiselyAPI.Infrastructure.Caching
{
    public interface IRedisCache
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    }
}
