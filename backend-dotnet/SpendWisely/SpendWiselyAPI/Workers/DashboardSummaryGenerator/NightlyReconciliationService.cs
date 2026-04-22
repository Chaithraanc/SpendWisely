using RabbitMQ.Client;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Infrastructure.Caching.MonthlySummary;
using SpendWiselyAPI.Infrastructure.DbContext;
using System.Text.Json;

namespace SpendWiselyAPI.Workers.DashboardSummaryGenerator
{
    public class NightlyReconciliationService : INightlyReconciliationService
    {
        private readonly IDashboardMonthlySummaryRepository _summaryRepo;
        private readonly IExpenseRepository _expenseRepo;
        private readonly IRedisService _redis;
        private readonly IEventPublisher _publisher;
        private readonly ILogger<NightlyReconciliationService> _logger;
        private readonly AppDbContext   _dbContext;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string MainExchange = "spendwisely.events";
        private readonly IServiceScopeFactory _scopeFactory;

        public NightlyReconciliationService(
            IDashboardMonthlySummaryRepository summaryRepo,
            IExpenseRepository expenseRepo,
            IRedisService redis,
            IEventPublisher publisher,
            ILogger<NightlyReconciliationService> logger,
            AppDbContext dbContext ,
            IServiceScopeFactory scopeFactory)
        {
            _summaryRepo = summaryRepo;
            _expenseRepo = expenseRepo;
            _redis = redis;
            _publisher = publisher;
            _logger = logger;
            _dbContext = dbContext;
            _scopeFactory = scopeFactory;
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            //test code
            //var sgTz = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            //var may1_2am = new DateTime(2026, 5, 1, 2, 0, 0, DateTimeKind.Unspecified);
            //var sgTime = TimeZoneInfo.ConvertTimeFromUtc(may1_2am, sgTz);
            //test code end

           var sgTime = DateTime.UtcNow.AddHours(8);

            // 1. Update SQL summary for current month 
            await UpdateDashboardSummaryAsync(sgTime);

            // 2. Sync Redis with SQL (fix drift)
            await SyncRedisWithSqlAsync(sgTime);

           

            // 3. If today is 1st → emit MonthlySummaryGenerated + reset Redis
           // if (sgTime.Day == 1 && !await _redis.HasMonthlySummaryGeneratedAsync(sgTime.AddMonths(-1).Year, sgTime.AddMonths(-1).Month))
                if (sgTime.Day == 1)
            {
                await EmitMonthlySummaryGeneratedEventAsync(sgTime);
                await ResetRedisCountersAsync();
            }
        }

        private async Task UpdateDashboardSummaryAsync(DateTime sgTime)
        {
            var month = sgTime.Month;
            var year = sgTime.Year;

            var totals = await _expenseRepo.GetAggregatedTotalsForMonthAsync(year, month);

            await _summaryRepo.UpsertMonthlySummaryAsync(year, month, totals);
                await _dbContext.SaveChangesAsync();
    
                _logger.LogInformation("Dashboard summary updated for {Month}/{Year} ",
                    month, year);

        }

        private async Task SyncRedisWithSqlAsync(DateTime sgTime)
        {
            var month = sgTime.Month;
            var year = sgTime.Year;

            var sqlSummary = await _summaryRepo.GetMonthlySummaryAsync(year, month);

            foreach (var userSummary in sqlSummary)
            {
                // total Spent for the user for current month
                if(userSummary.CategoryId == null)
                    await _redis.SetUserTotalAsync(userSummary.UserId, userSummary.TotalSpent);

                // categories
                if (userSummary.CategoryId.HasValue)
                {
                    await _redis.SetUserCategoryTotalAsync(userSummary.UserId, userSummary.CategoryId.Value, userSummary.TotalSpent);
                }
            }
        }

        private async Task EmitMonthlySummaryGeneratedEventAsync(DateTime sgTime)
        {
            var eventObj = new MonthlySummaryGeneratedEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = Guid.Empty, // No specific aggregate, this is a system-level event
                EventType = "MonthlySummaryGenerated",
                UserId = Guid.Empty, // System event, no specific user associated
                Year = sgTime.AddMonths(-1).Year,
                Month = sgTime.AddMonths(-1).Month,
                Timestamp = sgTime
            };

            await _channel.BasicPublishAsync(
                exchange: MainExchange,
                routingKey: "monthly.summary.generated",
                mandatory: true,
                // basicProperties: null,
                body: JsonSerializer.SerializeToUtf8Bytes(eventObj));

            _logger.LogInformation("MonthlySummaryGenerated event emitted for {Month}/{Year}",
                eventObj.Month, eventObj.Year);
        }

        private async Task ResetRedisCountersAsync()
        {
            await _redis.ResetAllUsersAsync();
            _logger.LogInformation("Redis counters reset for new month");
        }
    }
}
