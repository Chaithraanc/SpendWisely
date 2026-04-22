using Xunit;
using FluentAssertions;

namespace SpendWiselyAPI.Tests.Domain.Tests.Expense
{
    public class ExpenseEdgeCaseTests
    {
        [Fact]
        public void Should_Allow_Description_With_Normal_Text()
        {
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 10, "  Coffee  ", null);

            // Your domain does NOT trim, so whitespace stays
            expense.Description.Should().Be("  Coffee  ");
        }

        [Fact]
        public void Should_Allow_Category_To_Be_Set_To_Null_On_Update()
        {
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 10, "Lunch", Guid.NewGuid());

            expense.Update(10, "Lunch", null);

            expense.CategoryId.Should().BeNull();
        }

        [Fact]
        public void Should_Not_Change_CreatedAt_On_Update()
        {
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 10, "Lunch", null);

            var originalCreatedAt = expense.CreatedAt;

            expense.Update(20, "Dinner", null);

            expense.CreatedAt.Should().Be(originalCreatedAt);
        }
    }
}
