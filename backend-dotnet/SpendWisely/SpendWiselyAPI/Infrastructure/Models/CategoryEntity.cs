using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWiselyAPI.Infrastructure.Models
{
        public class CategoryEntity
        {
            public Guid Id { get; set; }               // From domain
            public Guid? UserId { get; set; }          // Optional FK
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

        // Navigation property (optional)
            [ForeignKey("UserId")]
            public virtual UserEntity User { get; set; } // Navigation for user-specific categories

        // Navigation for expenses
        public virtual ICollection<ExpenseEntity> Expenses { get; set; }// One category can have many expenses

        // Constructor to initialize the Expenses collection
        // This ensures that the Expenses collection is always initialized, preventing null reference issues when adding expenses to a category.
        public CategoryEntity()
        {
            Expenses = new HashSet<ExpenseEntity>();
        }
    }
}
