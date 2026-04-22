using FluentAssertions;
using Xunit;

namespace SpendWiselyAPI.Tests.Domain.Tests.Expense
{
    public class AssignCategoryTests
    {
        [Fact]
        public void Should_Assign_Category_When_Previously_Null()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Coffee", null);

            var newCategoryId = Guid.NewGuid();

            // Act
            expense.AssignCategory(newCategoryId);

            // Assert
            expense.CategoryId.Should().Be(newCategoryId);
        }

        [Fact]
        public void Should_Override_Existing_Category()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Lunch", Guid.NewGuid());

            var newCategoryId = Guid.NewGuid();

            // Act
            expense.AssignCategory(newCategoryId);

            // Assert
            expense.CategoryId.Should().Be(newCategoryId);
        }

        [Fact]
        public void Should_Not_Throw_When_Assigning_Category()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Snacks", null);

            var categoryId = Guid.NewGuid();

            // Act
            Action act = () => expense.AssignCategory(categoryId);

            // Assert
            act.Should().NotThrow();
        }
    }
}

