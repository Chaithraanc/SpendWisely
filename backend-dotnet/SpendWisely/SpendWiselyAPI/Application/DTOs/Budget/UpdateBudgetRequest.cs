using SpendWiselyAPI.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SpendWiselyAPI.Application.DTOs.Budget
{
    public class UpdateBudgetRequest
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public Guid? CategoryId { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Range(2000, 2100)]
        public int Year { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public SpendWiselyAPI.Domain.Entities.Budget ToDomain(Guid id) =>
            new SpendWiselyAPI.Domain.Entities.Budget
            (
                Id = id,
                UserId = UserId,
                CategoryId = CategoryId,
                Amount = Amount,   
                Month = Month,
                Year = Year
                


            );
    }
}
