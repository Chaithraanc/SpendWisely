namespace SpendWiselyAPI.Domain.Entities
{
    public class Budget
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid? CategoryId { get; private set; }
        public decimal Amount { get; private set; }
        public int Month { get; private set; }
        public int Year { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Constructor for creating new budgets
        public Budget(Guid userId , Guid? categoryId, decimal amount, int month, int year) { 
            Id = Guid.NewGuid();
            UserId = userId;
            CategoryId = categoryId;
            SetAmount(amount);
            Month = month;
            Year = year;
            CreatedAt = DateTime.UtcNow;
        }

        // Hydration constructor for re-hydrating from persistence store
        public Budget(Guid id, Guid userId, Guid? categoryId, decimal amount, int month, int year, DateTime createdAt, DateTime? updatedAt)
        {
            Id = id;
            UserId = userId;
            CategoryId = categoryId;
            SetAmount(amount);
            Month = month;
            Year = year;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
        // Overloaded constructor for update scenarios where Id is known
        public Budget(Guid budgetId, Guid userId,  Guid? categoryId, decimal amount, int month, int year)
        {
            Id = budgetId;
            UserId = userId;
            SetAmount(amount);
            CategoryId = categoryId;
           
            Month = month;
            Year = year;
            
        }

        private void SetAmount(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            Amount = amount;
        }

            public void Update(decimal amount, Guid? categoryId)
            {
                SetAmount(amount);
                CategoryId = categoryId;
                UpdatedAt = DateTime.UtcNow;
        }

    }

}
