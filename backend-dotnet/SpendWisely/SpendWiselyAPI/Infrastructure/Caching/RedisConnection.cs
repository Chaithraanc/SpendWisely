using StackExchange.Redis;

namespace SpendWiselyAPI.Infrastructure.Caching
{


    public static class RedisConnection
    {
        private static readonly Lazy<ConnectionMultiplexer> lazyConnection =
            new Lazy<ConnectionMultiplexer>(() =>
            {
                var options = new ConfigurationOptions
                {
                    EndPoints = { { "redis-10780.crce214.us-east-1-3.ec2.cloud.redislabs.com", 10780 } },
                    User = "default",
                    Password = "Qk2MWKW9Xfs0IojJ6XpBjBbDBa4bR1yu",
                    Ssl = true,
                    SslHost = "redis-10780.crce214.us-east-1-3.ec2.cloud.redislabs.com",
                    AbortOnConnectFail = false
                };

                return ConnectionMultiplexer.Connect(options);
            });

        public static ConnectionMultiplexer Connection => lazyConnection.Value;
    }

}
