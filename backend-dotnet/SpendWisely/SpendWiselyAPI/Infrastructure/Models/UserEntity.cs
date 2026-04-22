using SpendWiselyAPI.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWiselyAPI.Infrastructure.Models
{
  
        public class UserEntity
        {
            public Guid Id { get; set; }                 // From domain
            public string Name { get; set; }
            public string Email { get; set; }
            public string PasswordHash { get; set; }
            public string Role { get; set; }             // "User" / "Admin"
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime? RefreshTokenExpiry { get; set; }


        public virtual ICollection<CategoryEntity> Categories { get; set; } // One user can have many categories (including user-specific categories)
        public virtual ICollection<ExpenseEntity> Expenses { get; set; }// One user can have many expenses


        // Constructor to initialize the navigation properties
        public UserEntity()
            {
                Categories = new HashSet<CategoryEntity>();
                Expenses = new HashSet<ExpenseEntity>();
            }
        }
}
