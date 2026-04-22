using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Services;
using SpendWiselyAPI.Infrastructure.DbContext;
using Xunit;

namespace SpendWiselyAPI.Tests.Application.Tests.Expense
{
    public class ExpenseService_GetExpenseByIdTests
    {
        private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
        private readonly Mock<IEventStoreRepository> _eventStoreMock = new();
        private readonly Mock<IEventPublisher> _eventPublisherMock = new();
        private readonly Mock<IOutboxEventRepository> _outboxRepoMock = new();
        private readonly Mock<ILogger<ExpenseService>> _loggerMock = new();

        private readonly AppDbContext _dbContext;
        private readonly ExpenseService _service;

        public ExpenseService_GetExpenseByIdTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            _service = new ExpenseService(
                _expenseRepoMock.Object,
                _userRepoMock.Object,
                _categoryRepoMock.Object,
                _eventStoreMock.Object,
                _eventPublisherMock.Object,
                _outboxRepoMock.Object,
                _dbContext,
                _loggerMock.Object
            );
        }

        // -------------------------------------------------------
        // 1. Should return expense when found
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Return_Expense_When_Found()
        {
            // Arrange
            var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 20, "Coffee", null);

            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(expenseId))
                            .ReturnsAsync(expense);

            // Act
            var result = await _service.GetExpenseByIdAsync(expenseId);

            // Assert
            result.Should().NotBeNull();
            result.Description.Should().Be("Coffee");
        }

        // -------------------------------------------------------
        // 2. Should throw when expense not found
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Throw_When_Expense_Not_Found()
        {
            // Arrange
            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((SpendWiselyAPI.Domain.Entities.Expense)null);

            // Act
            Func<Task> act = () => _service.GetExpenseByIdAsync(Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Expense not found");
        }
    }
}
