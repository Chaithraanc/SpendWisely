using System.Data.Common;

namespace SpendWiselyAPI.Domain.Entities
{
    // Domain/Entities/Expense.cs
    // This class represents the core business entity for an expense
    // in the Spend Wisely application.

    public class Expense
    {
        public Guid Id { get; private set; } // Unique identifier for the expense
        public Guid UserId { get; private set; }// Identifier for the user who created the expense
        public decimal Amount { get; private set; }
        public string Description { get; private set; }
        public Guid? CategoryId { get; private set; }

        public DateTime CreatedAt { get; private set; } 

        public DateTime? UpdatedAt { get; private set; }



        // Constructor for creating a new expense (client code)
        public Expense(Guid userId, decimal amount, string description, Guid? categoryId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            SetAmount(amount);
            SetDescription(description);
            CategoryId = categoryId == null ? null : categoryId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = null;
        }
        // private Expense() { }

        // Hydration constructor for re-hydrating from persistence store
        public Expense(Guid id, Guid userId, decimal amount, string description, Guid? categoryId, DateTime createdAt, DateTime? updatedAt)
        {
            Id = id;
            UserId = userId;
            SetAmount(amount);
            SetDescription(description);
            CategoryId = categoryId;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public void Update(decimal amount, string description, Guid? categoryId)
        {
            SetAmount(amount);
            SetDescription(description);
            CategoryId = categoryId;
            UpdatedAt = DateTime.UtcNow;
        }
        public void AssignCategory(Guid categoryId)
        {

            CategoryId = categoryId;
        }
        private void SetAmount(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            Amount = amount;
        }

        private void SetDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty");

            Description = description;
        }
    }
}
