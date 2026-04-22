using Quartz;

namespace SpendWiselyAPI.Workers.DashboardSummaryGenerator
{
    public class NightlyReconciliationJob : IJob
    {
        private readonly INightlyReconciliationService _service;
        private readonly ILogger<NightlyReconciliationJob> _logger;

        public NightlyReconciliationJob(
            INightlyReconciliationService service,
            ILogger<NightlyReconciliationJob> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Nightly reconciliation job started at {Time}", DateTime.UtcNow);

            await _service.RunAsync();

            _logger.LogInformation("Nightly reconciliation job completed at {Time}", DateTime.UtcNow);
        }
    }
}
