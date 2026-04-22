using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Services;
using SpendWiselyAPI.Infrastructure.DbContext;
using Xunit;

namespace SpendWiselyAPI.Tests.Application.Tests.Expense
{
    public class ExpenseService_GetExpensesByUserTests
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

        public ExpenseService_GetExpensesByUserTests()
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
        // 1. Should return list of expenses for user
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Return_Expenses_For_User()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var expenses = new List<SpendWiselyAPI.Domain.Entities.Expense>
            {
                new SpendWiselyAPI.Domain.Entities.Expense(userId, 10, "Tea", null),
                new SpendWiselyAPI.Domain.Entities.Expense(userId, 20, "Snacks", null)
            };

            _expenseRepoMock.Setup(r => r.GetExpensesByUserAsync(userId))
                            .ReturnsAsync(expenses);

            // Act
            var result = await _service.GetExpensesByUserAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result[0].Description.Should().Be("Tea");
            result[1].Description.Should().Be("Snacks");
        }

        // -------------------------------------------------------
        // 2. Should return empty list when user has no expenses
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Return_Empty_List_When_No_Expenses()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(UserTestFactory.Create(userId));

            // Act
            var result = await _service.GetExpensesByUserAsync(userId);

            // Assert
            result.Should().BeEmpty();
        }
    }

}
