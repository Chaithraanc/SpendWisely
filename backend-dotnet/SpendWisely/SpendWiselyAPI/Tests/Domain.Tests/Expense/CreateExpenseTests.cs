using Xunit;
using SpendWiselyAPI.Domain.Entities;
using FluentAssertions;
namespace SpendWiselyAPI.Tests.Domain.Tests.Expense
{
    public class CreateExpenseTests
    {
        [Fact]
        public void Should_Create_Expense_With_Valid_Data()
        {
            // Arrange
            var userId = Guid.NewGuid();
            decimal amount = 100;
            string description = "Coffee";
            Guid? categoryId = Guid.NewGuid();

            // Act
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, amount, description, categoryId);

            // Assert
            expense.Id.Should().NotBeEmpty();
            expense.UserId.Should().Be(userId);
            expense.Amount.Should().Be(amount);
            expense.Description.Should().Be(description);
            expense.CategoryId.Should().Be(categoryId);

            expense.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            expense.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Should_Create_Expense_With_Category_Null()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 50, "Snacks", null);

            // Assert
            expense.CategoryId.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Should_Throw_When_Amount_Is_Invalid(decimal invalidAmount)
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            Action act = () => new SpendWiselyAPI.Domain.Entities.Expense(userId, invalidAmount, "Test", null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Amount must be greater than zero");
        }

        [Fact]
        public void Should_Throw_When_Description_Is_Empty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            Action act = () => new SpendWiselyAPI.Domain.Entities.Expense(userId, 10, "", null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Description cannot be empty");
        }

        [Fact]
        public void Should_Throw_When_Description_Is_Whitespace()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            Action act = () => new SpendWiselyAPI.Domain.Entities.Expense(userId, 10, "   ", null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Description cannot be empty");
        }

        [Fact]
        public void Should_Set_CreatedAt_To_UtcNow()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Lunch", null);

            // Assert
            expense.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
    }
}
