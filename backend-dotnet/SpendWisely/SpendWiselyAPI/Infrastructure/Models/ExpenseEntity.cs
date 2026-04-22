using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWiselyAPI.Infrastructure.Models
{

    // Infrastructure/Models/ExpenseEntity.cs
    // This class represents the database entity for an expense.
    // It is used by Entity Framework Core to map to the "Expenses" table in the
    // database.
    public class ExpenseEntity
    {
        public Guid Id { get; set; }       
        public Guid UserId { get; set; }      

        public decimal Amount { get; set; }
        public string Description { get; set; }
        public Guid? CategoryId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for the related Category
        [ForeignKey("CategoryId")]
        public CategoryEntity Category { get; set; }

        [ForeignKey("UserId")]
        public UserEntity User { get; set; }
    }

}
