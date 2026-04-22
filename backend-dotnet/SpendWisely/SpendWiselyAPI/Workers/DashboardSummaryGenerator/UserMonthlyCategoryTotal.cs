using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWiselyAPI.Workers.DashboardSummaryGenerator
{
    [NotMapped]
    public class UserMonthlyCategoryTotal
    {
        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; } // null = total
        public decimal TotalSpent { get; set; }
    }
}
