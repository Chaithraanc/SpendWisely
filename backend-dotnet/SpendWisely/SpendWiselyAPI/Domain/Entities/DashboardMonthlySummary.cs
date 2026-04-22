namespace SpendWiselyAPI.Domain.Entities
{
    public class DashboardMonthlySummary
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public int Month { get; private set; }
        public int Year { get; private set; }

        // NULL = total monthly summary
        public Guid? CategoryId { get; private set; }

        public decimal TotalSpent { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

       
        private DashboardMonthlySummary() { }

        // Constructor for inserts (Go service monthly flush)
        public DashboardMonthlySummary(
            Guid id,
            Guid userId,
            int month,
            int year,
            Guid? categoryId,
            decimal totalSpent)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Month = month;
            Year = year;
            CategoryId = categoryId;
            TotalSpent = totalSpent;
            CreatedAt = DateTime.UtcNow;
        }
        // Hydration constructor for re-hydrating from persistence store
        public DashboardMonthlySummary(
            Guid id,
            Guid userId,
            int month,
            int year,
            Guid? categoryId,
            decimal totalSpent,
            DateTime createdAt,
            DateTime? updatedAt)
        {
            Id = id;
            UserId = userId;
            Month = month;
            Year = year;
            CategoryId = categoryId;
            TotalSpent = totalSpent;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        // Update method
        public void UpdateTotal(decimal newTotal )
        {
            TotalSpent = newTotal;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
