using FluentAssertions;
using Xunit;

namespace SpendWiselyAPI.Tests.Domain.Tests.Expense
{
    public class HydrationConstructorTests
    {
        [Fact]
        public void Should_Hydrate_Expense_With_Valid_Data()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-2);
            var updatedAt = DateTime.UtcNow.AddDays(-1);
            var categoryId = Guid.NewGuid();

            // Act
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(
                id,
                userId,
                100,
                "Groceries",
                categoryId,
                createdAt,
                updatedAt
            );

            // Assert
            expense.Id.Should().Be(id);
            expense.UserId.Should().Be(userId);
            expense.Amount.Should().Be(100);
            expense.Description.Should().Be("Groceries");
            expense.CategoryId.Should().Be(categoryId);
            expense.CreatedAt.Should().Be(createdAt);
            expense.UpdatedAt.Should().Be(updatedAt);
        }

        [Fact]
        public void Should_Throw_When_Hydrating_With_Invalid_Amount()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();

            Action act = () => new SpendWiselyAPI.Domain.Entities.Expense(
                id,
                userId,
                0,
                "Test",
                null,
                DateTime.UtcNow,
                null
            );

            act.Should().Throw<ArgumentException>()
                .WithMessage("Amount must be greater than zero");
        }

        [Fact]
        public void Should_Throw_When_Hydrating_With_Empty_Description()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();

            Action act = () => new SpendWiselyAPI.Domain.Entities.Expense(
                id,
                userId,
                10,
                "",
                null,
                DateTime.UtcNow,
                null
            );

            act.Should().Throw<ArgumentException>()
                .WithMessage("Description cannot be empty");
        }
    }
}
