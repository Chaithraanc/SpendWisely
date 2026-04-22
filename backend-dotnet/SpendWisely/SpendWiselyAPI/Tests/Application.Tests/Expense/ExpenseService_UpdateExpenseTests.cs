using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Services;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Events.Models;
using Xunit;

namespace SpendWiselyAPI.Tests.Application.Tests.Expense
{
    public class ExpenseService_UpdateExpenseTests
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

        public ExpenseService_UpdateExpenseTests()
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
        // 1. Should update expense and save
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Update_Expense_And_Save()
        {
            // Arrange
            var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 20, "Coffee", null);

            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(expenseId))
                            .ReturnsAsync(expense);

            // Act
            var result = await _service.UpdateExpenseAsync(expenseId, 40, "Dinner", Guid.NewGuid());

            // Assert
            result.Amount.Should().Be(40);
            result.Description.Should().Be("Dinner");

            _expenseRepoMock.Verify(r => r.UpdateExpenseAsync(expense), Times.Once);
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
            Func<Task> act = () => _service.UpdateExpenseAsync(Guid.NewGuid(), 20, "Test", null);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Expense not found");
        }

        // -------------------------------------------------------
        // 3. Should throw when category does not exist
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Throw_When_Category_Not_Found()
        {
            // Arrange
            var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 20, "Coffee", null);
            var categoryId = Guid.NewGuid();

            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(expenseId))
                            .ReturnsAsync(expense);

            _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(categoryId))
                             .ReturnsAsync((Category)null);

            // Act
            Func<Task> act = () => _service.UpdateExpenseAsync(expenseId, 20, "Coffee", categoryId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Category not found ,please create category and assign to expense");
        }

        // -------------------------------------------------------
        // 4. Should create outbox event on update
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Create_Outbox_Event_On_Update()
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
            await _service.UpdateExpenseAsync(expenseId, 30, "Updated", null);

            // Assert
            capturedEvent.Should().NotBeNull();
            capturedEvent!.EventType.Should().Be("ExpenseUpdated");
            capturedEvent.Payload.Should().Contain("Updated");
        }

        // -------------------------------------------------------
        // 5. Should rollback transaction on failure
        // -------------------------------------------------------
        [Fact]
        public async Task Should_Rollback_When_Update_Fails()
        {
            // Arrange
            var expenseId = Guid.NewGuid();
            var expense = new SpendWiselyAPI.Domain.Entities.Expense(Guid.NewGuid(), 20, "Coffee", null);

            _expenseRepoMock.Setup(r => r.GetExpenseByIdAsync(expenseId))
                            .ReturnsAsync(expense);

            _expenseRepoMock.Setup(r => r.UpdateExpenseAsync(expense))
                            .ThrowsAsync(new Exception("DB error"));

            // Act
            Func<Task> act = () => _service.UpdateExpenseAsync(expenseId, 20, "Coffee", null);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("DB error");

            // Outbox should NOT be saved
            _outboxRepoMock.Verify(r => r.AddOutboxEventAsync(It.IsAny<OutboxEvent>()), Times.Never);
        }
    }
}
