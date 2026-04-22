using FluentAssertions;
using Xunit;

namespace SpendWiselyAPI.Tests.Domain.Tests.Expense
{
    public class UpdateExpenseTests
    {
        [Fact]
        public void Should_Update_Expense_With_Valid_Data()
        {
            // Arrange
            var userId = Guid.NewGuid();
           // var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 50, "Snacks", null);

            decimal newAmount = 100;
            string newDescription = "Groceries";
            Guid? newCategoryId = Guid.NewGuid();

            // Act
            expense.Update(newAmount, newDescription, newCategoryId);

            // Assert
            expense.Amount.Should().Be(newAmount);
            expense.Description.Should().Be(newDescription);
            expense.CategoryId.Should().Be(newCategoryId);

            expense.UpdatedAt.Should().NotBeNull();
            expense.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Should_Update_Category_When_Previously_Null()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Coffee", null);

            Guid newCategoryId = Guid.NewGuid();

            // Act
            expense.Update(20, "Coffee", newCategoryId);

            // Assert
            expense.CategoryId.Should().Be(newCategoryId);
        }

        [Fact]
        public void Should_Allow_Category_To_Be_Set_To_Null()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Coffee", Guid.NewGuid());

            // Act
            expense.Update(20, "Coffee", null);

            // Assert
            expense.CategoryId.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Should_Throw_When_Updating_With_Invalid_Amount(decimal invalidAmount)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Lunch", null);

            // Act
            Action act = () => expense.Update(invalidAmount, "Lunch", null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Amount must be greater than zero");
        }

        [Fact]
        public void Should_Throw_When_Updating_With_Empty_Description()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Lunch", null);

            // Act
            Action act = () => expense.Update(20, "", null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Description cannot be empty");
        }

        [Fact]
        public void Should_Throw_When_Updating_With_Whitespace_Description()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Lunch", null);

            // Act
            Action act = () => expense.Update(20, "   ", null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Description cannot be empty");
        }

        [Fact]
        public void Should_Update_UpdatedAt_Timestamp()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Lunch", null);

            // Act
            expense.Update(25, "Lunch Updated", null);

            // Assert
            expense.UpdatedAt.Should().NotBeNull();
            expense.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
    }
}
