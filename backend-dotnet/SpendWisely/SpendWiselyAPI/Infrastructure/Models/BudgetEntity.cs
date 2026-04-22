using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWiselyAPI.Infrastructure.Models
{
    public class BudgetEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal Amount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for the related Category
        [ForeignKey("CategoryId")]
        public CategoryEntity Category { get; set; }

        [ForeignKey("UserId")]
        public UserEntity User { get; set; }
    }
}
