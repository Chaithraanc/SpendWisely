namespace SpendWiselyAPI.Application.DTOs.Expenses
{
    public class CreateExpenseRequest
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public Guid? CategoryId { get; set; } = Guid.Empty;
    }
}
