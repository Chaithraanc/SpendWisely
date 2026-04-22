using Quartz;

namespace SpendWiselyAPI.Workers.DashboardSummaryGenerator
{
    public class StartupJobRunner : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public StartupJobRunner(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            await scheduler.TriggerJob(
                new JobKey("NightlyReconciliationJob"),
                cancellationToken
            );
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
