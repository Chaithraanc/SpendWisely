using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace SpendWiselyAPI.Infrastructure.Messaging
{
    public class MessagingPolicies
    {
        public AsyncRetryPolicy RetryPolicy { get; }
        public AsyncCircuitBreakerPolicy CircuitBreakerPolicy { get; }

        public MessagingPolicies(ILogger<MessagingPolicies> logger)
        {
            RetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    5,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (ex, ts, attempt, ctx) =>
                    {
                        logger.LogWarning(ex, "Retry {Attempt}/5 after {Delay}s", attempt, ts.TotalSeconds);
                    });

            CircuitBreakerPolicy = Policy
                .Handle<Exception>()
               .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (ex, ts) =>
                    {
                        logger.LogWarning(ex,
                            "circuit breaker OPEN for {Duration}s due to repeated failures.",
                            ts.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("circuit breaker RESET. Resuming normal operation.");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("circuit breaker HALF-OPEN. Testing few requests.");
                    });

        }
    }

}
