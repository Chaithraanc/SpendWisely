using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Services;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Events.Models;
using Xunit;

namespace SpendWiselyAPI.Tests.Application.Tests.Expense
{
    public class ExpenseService_DeleteExpenseTests
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

        public ExpenseService_DeleteExpenseTests()
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
        // 1. Should delete expense and save outbox event
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Delete_Expense_And_Save_Outbox_Event()
        {
            // Arrange
            var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 50, "Snacks", Guid.NewGuid());

            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(expenseId))
                            .ReturnsAsync(expense);

            // Act
            await _service.DeleteExpenseAsync(expenseId);

            // Assert
            _expenseRepoMock.Verify(r => r.DeleteExpenseAsync(expenseId), Times.Once);
            _outboxRepoMock.Verify(r => r.AddOutboxEventAsync(It.IsAny<OutboxEvent>()), Times.Once);
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
            Func<Task> act = () => _service.DeleteExpenseAsync(Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Expense not found");
        }

        // -------------------------------------------------------
        // 3. Should create outbox event on delete
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Create_Outbox_Event_On_Delete()
        {
            // Arrange
            var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 20, "Coffee", null);

            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(expenseId))
                            .ReturnsAsync(expense);

            OutboxEvent? capturedEvent = null;

            _outboxRepoMock.Setup(r => r.AddOutboxEventAsync(It.IsAny<OutboxEvent>()))
                           .Callback<OutboxEvent>(evt => capturedEvent = evt)
                           .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteExpenseAsync(expenseId);

            // Assert
            capturedEvent.Should().NotBeNull();
            capturedEvent!.EventType.Should().Be("ExpenseDeleted");
            capturedEvent.Payload.Should().Contain("Coffee");
        }

        // -------------------------------------------------------
        // 4. Should rollback transaction on failure
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Rollback_When_Delete_Fails()
        {
            // Arrange
            var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 20, "Coffee", null);

            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(expenseId))
                            .ReturnsAsync(expense);

            _expenseRepoMock.Setup(r => r.DeleteExpenseAsync(expenseId))
                            .ThrowsAsync(new Exception("DB error"));

            // Act
            Func<Task> act = () => _service.DeleteExpenseAsync(expenseId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("DB error");

            // Outbox should NOT be saved
            _outboxRepoMock.Verify(r => r.AddOutboxEventAsync(It.IsAny<OutboxEvent>()), Times.Never);
        }
    }
}
