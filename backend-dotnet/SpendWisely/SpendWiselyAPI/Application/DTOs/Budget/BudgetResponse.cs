namespace SpendWiselyAPI.Application.DTOs.Budget
{
    public class BudgetResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal Amount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public static BudgetResponse FromDomain(SpendWiselyAPI.Domain.Entities.Budget budget)
        {
            return new BudgetResponse
            {
                Id = budget.Id,
                UserId = budget.UserId,
                CategoryId = budget.CategoryId,
                Amount = budget.Amount,
                Month = budget.Month,
                Year = budget.Year,
                CreatedAt = budget.CreatedAt,
                UpdatedAt = budget.UpdatedAt
            };
        }
    }
}
