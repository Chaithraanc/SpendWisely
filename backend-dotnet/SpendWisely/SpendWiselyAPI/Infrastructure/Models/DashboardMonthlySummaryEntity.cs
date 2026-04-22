namespace SpendWiselyAPI.Infrastructure.Models
{
    public class DashboardMonthlySummaryEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get;  set; }
        public int Month { get; set; }
        public int Year { get; set; }

        // NULL = total monthly summary
        public Guid? CategoryId { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


        // Update method
        public void UpdateEntityTotal(decimal newTotal)
        {
            TotalSpent = newTotal;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
