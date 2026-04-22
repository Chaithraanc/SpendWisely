using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Services;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Events.Models;
using Xunit;
using Moq;


namespace SpendWiselyAPI.Tests.Application.Tests.Expense
{
    public class ExpenseService_CreateExpenseTests
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

        public ExpenseService_CreateExpenseTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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
        // 1. Should create expense and save to repository
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Create_Expense_And_Save_To_Repository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(UserTestFactory.Create(userId));


            // Act
            var result = await _service.CreateExpenseAsync(userId, 50, "Snacks", null);

            // Assert
            result.Should().NotBeNull();
            result.Amount.Should().Be(50);
            result.Description.Should().Be("Snacks");

            _expenseRepoMock.Verify(r => r.AddExpenseAsync(It.IsAny<SpendWiselyAPI.Domain.Entities.Expense>()), Times.Once);
            _outboxRepoMock.Verify(r => r.AddOutboxEventAsync(It.IsAny<SpendWiselyAPI.Infrastructure.Events.Models.OutboxEvent>()), Times.Once);
        }

        // -------------------------------------------------------
        // 2. Should throw when user not found
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Throw_When_User_Not_Found()
        {
            // Arrange
                var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetUserByIdAsync(userId))
              .ReturnsAsync(UserTestFactory.Create(userId));


            // Act
            Func<Task> act = () => _service.CreateExpenseAsync(Guid.NewGuid(), 10, "Test", null);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User not found");
        }

        // -------------------------------------------------------
        // 3. Should throw when category does not exist if categoryId provided by user
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Throw_When_Category_Not_Found()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

          
            _userRepoMock.Setup(r => r.GetUserByIdAsync(userId))
              .ReturnsAsync(UserTestFactory.Create(userId));

            _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(categoryId))
                             .ReturnsAsync((Category)null);

            // Act
            Func<Task> act = () => _service.CreateExpenseAsync(userId, 10, "Test", categoryId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Category not found , please create category or let AI categorize it");
        }

        // -------------------------------------------------------
        // 4. Should create outbox event
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Create_Outbox_Event_When_Expense_Created()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetUserByIdAsync(userId))
              .ReturnsAsync(UserTestFactory.Create(userId));

            OutboxEvent? capturedEvent = null;

            _outboxRepoMock.Setup(r => r.AddOutboxEventAsync(It.IsAny<OutboxEvent>()))
                           .Callback<OutboxEvent>(evt => capturedEvent = evt)
                           .Returns(Task.CompletedTask);

            // Act
            await _service.CreateExpenseAsync(userId, 20, "Coffee", null);

            // Assert
            capturedEvent.Should().NotBeNull();
            capturedEvent!.EventType.Should().Be("ExpenseCreated");
            capturedEvent.Payload.Should().Contain("Coffee");
        }

        // -------------------------------------------------------
        // 5. Should rollback transaction on failure
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Rollback_Transaction_When_Repository_Fails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetUserByIdAsync(userId))
              .ReturnsAsync(UserTestFactory.Create(userId));

            _expenseRepoMock.Setup(r => r.AddExpenseAsync(It.IsAny<SpendWiselyAPI.Domain.Entities.Expense>()))
                            .ThrowsAsync(new Exception("DB error"));

            // Act
            Func<Task> act = () => _service.CreateExpenseAsync(userId, 10, "Test", null);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("DB error");

            // Outbox should NOT be saved
            _outboxRepoMock.Verify(r => r.AddOutboxEventAsync(It.IsAny<OutboxEvent>()), Times.Never);
        }
    }

    

}
