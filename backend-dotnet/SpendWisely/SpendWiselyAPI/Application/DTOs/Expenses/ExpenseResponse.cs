namespace SpendWiselyAPI.Application.DTOs.Expenses
{
    public class ExpenseResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public Guid? CategoryId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
