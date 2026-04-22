namespace SpendWiselyAPI.Application.DTOs.Expenses
{
    public class UpdateExpenseRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public Guid? CategoryId { get; set; }
    }
}
