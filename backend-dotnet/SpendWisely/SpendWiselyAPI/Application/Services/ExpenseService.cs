using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Events.Models;
using SpendWiselyAPI.Infrastructure.Messaging.Consumers;
using SpendWiselyAPI.Infrastructure.MongoDB.Repositories;
using System.Text.Json;
using Microsoft.Extensions.Logging;


namespace SpendWiselyAPI.Application.Services
{
  
        public class ExpenseService : IExpenseService
        {
            private readonly IExpenseRepository _expenseRepository;
            private readonly IUserRepository _userRepository;
            private readonly ICategoryRepository _categoryRepository;
            private readonly IEventStoreRepository _eventStore;
            private readonly IEventPublisher _eventPublisher;
            private readonly IOutboxEventRepository _outboxEventRepository;
            private readonly AppDbContext _dbContext;

        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(
                IExpenseRepository expenseRepository,
                IUserRepository userRepository,
                ICategoryRepository categoryRepository , IEventStoreRepository eventStoreRepository, IEventPublisher eventPublisher, IOutboxEventRepository outboxEventRepository,
                AppDbContext dbContext ,ILogger<ExpenseService> logger)
        {
            _expenseRepository = expenseRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _eventStore = eventStoreRepository;
            _eventPublisher = eventPublisher;
            _outboxEventRepository = outboxEventRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Expense> CreateExpenseAsync(Guid userId, decimal amount, string description, Guid? categoryId)
            {
            _logger.LogInformation("Creating expense for user {UserId} with amount {Amount}", userId, amount);
            // Validate user
            User user = await _userRepository.GetUserByIdAsync(userId) ?? throw new Exception("User not found");
            _logger.LogInformation("User {UserId} found, proceeding with expense creation", userId);
            // Validate category (if provided by user or by default category is null)
            if (categoryId.HasValue)
            {
                var category = await _categoryRepository.GetCategoryByIdAsync(categoryId.Value);
                if (category == null)
                    throw new Exception("Category not found , please create category or let AI categorize it");
            }

                // Create domain object
            var expense = new Expense(userId, amount, description, categoryId);
            // Start transaction to ensure atomicity of expense creation and outbox event insertion
            _logger.LogInformation("Starting transaction for expense creation and outbox event insertion");
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Insert expense into SQL Server
                  _logger.LogInformation("Inserting expense into database for user {UserId} with amount {Amount}", userId, amount);
                await _expenseRepository.AddExpenseAsync(expense);

                //Create expense created event
                var expenseEvent = new ExpenseEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = "ExpenseCreated",
                    ExpenseId = expense.Id,
                    UserId = expense.UserId,
                    Amount = expense.Amount,
                    Description = expense.Description,
                    CategoryId = expense.CategoryId,
                    Timestamp = DateTime.UtcNow
                };
                // 2. Create outbox event 
                var outboxEvent = new OutboxEvent
                {
                    Id = expenseEvent.EventId,
                    EventType = expenseEvent.EventType,
                    AggregateId = expenseEvent.ExpenseId,
                    Payload = JsonSerializer.Serialize(expenseEvent),
                    Processed = false,
                    CreatedAt = expenseEvent.Timestamp
                };
                // 3. Save outbox event in the same transaction
                await _outboxEventRepository.AddOutboxEventAsync(outboxEvent);
                _logger.LogInformation("Expense created and outbox event saved for user {UserId} with amount {Amount}", userId, amount);
                await _dbContext.SaveChangesAsync();
                // 4. Commit transaction to ensure both operations succeed or fail together
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully for expense creation and outbox event insertion for user {UserId} with amount {Amount}", userId, amount);
            }
            catch
            {
                // Rollback transaction if any operation fails
                await transaction.RollbackAsync();
                _logger.LogError("Transaction rolled back due to an error during expense creation and outbox event insertion for user {UserId} with amount {Amount}", userId, amount);
                throw;
            }

      
            return expense;
            }

            public async Task<List<Expense>> GetExpensesByUserAsync(Guid userId)
            {
                return await _expenseRepository.GetExpensesByUserAsync(userId);
            }

            public async Task<Expense> GetExpenseByIdAsync(Guid id)
            {
                var expense =  await _expenseRepository.GetExpenseByIdAsync(id);
                if(expense == null)
                    throw new Exception("Expense not found");
                return expense;
        }

            public async Task<Expense> UpdateExpenseAsync(Guid expenseId, decimal amount, string description, Guid? categoryId)
            {
                var expense = await _expenseRepository.GetExpenseByIdAsync(expenseId);

                if (expense == null)
                    throw new Exception("Expense not found");

                // Optional: validate category again
                if (categoryId.HasValue)
                {
                    var category = await _categoryRepository.GetCategoryByIdAsync(categoryId.Value);
                    if (category == null)
                        throw new Exception("Category not found ,please create category and assign to expense");
                }

                // Update domain object
                expense.Update(amount, description, categoryId);


            // Start transaction to ensure atomicity of expense updation and outbox event insertion
           await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
               
                _logger.LogInformation("Starting transaction for expense update and outbox event insertion for expense {ExpenseId} with amount {Amount}", expenseId, amount);
                //Update expense  event
                var expenseEvent = new ExpenseEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = "ExpenseUpdated",
                    ExpenseId = expense.Id,
                    UserId = expense.UserId,
                    Amount = expense.Amount,
                    Description = expense.Description,
                    CategoryId = expense.CategoryId,
                    Timestamp = DateTime.UtcNow
                };
                //  Create outbox event 
                var outboxEvent = new OutboxEvent
                {
                    Id = expenseEvent.EventId,
                    EventType = expenseEvent.EventType,
                    AggregateId = expenseEvent.ExpenseId,
                    Payload = JsonSerializer.Serialize(expenseEvent),
                    Processed = false,
                    CreatedAt = expenseEvent.Timestamp
                };
                // 1. Update expense into SQL Server

                await _expenseRepository.UpdateExpenseAsync(expense);
                // 2. Save outbox event in the same transaction
                await _outboxEventRepository.AddOutboxEventAsync(outboxEvent);

                // 3. Save changes to ensure both expense update and outbox event insertion are part of the same transaction
                await _dbContext.SaveChangesAsync();
                // 4. Commit transaction to ensure both operations succeed or fail together
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully for expense update and outbox event insertion for expense {ExpenseId} with amount {Amount}", expenseId, amount);
            }
            catch
            {
                // Rollback transaction if any operation fails
                await transaction.RollbackAsync();
                throw;
            }



            return expense;

        }

        public async Task DeleteExpenseAsync(Guid expenseId)
            {

            var expense = await _expenseRepository.GetExpenseByIdAsync(expenseId);

            if (expense == null)
                throw new Exception("Expense not found");

            // Start transaction to ensure atomicity of expense deletion and outbox event insertion
           await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Dleete expense from SQL Server
                _logger.LogInformation("Starting transaction for expense deletion and outbox event insertion for expense {ExpenseId} with amount {Amount}", expenseId, expense.Amount);
                await _expenseRepository.DeleteExpenseAsync(expenseId);

                //Delete expense  event
                var expenseEvent = new ExpenseEvent
                {
                    EventId = Guid.NewGuid(),
                    ExpenseId = expense.Id,
                    EventType = "ExpenseDeleted",
                    UserId = expense.UserId,
                    Amount = expense.Amount,
                    Description = expense.Description,
                    CategoryId = expense.CategoryId,
                    Timestamp = DateTime.UtcNow
                };
                // 2. Create outbox event 
                var outboxEvent = new OutboxEvent
                {
                    Id = expenseEvent.EventId,
                    EventType = expenseEvent.EventType,
                    AggregateId = expenseEvent.ExpenseId,
                    Payload = JsonSerializer.Serialize(expenseEvent),
                    Processed = false,
                    CreatedAt = expenseEvent.Timestamp
                };
                // 3. Save outbox event in the same transaction
                await _outboxEventRepository.AddOutboxEventAsync(outboxEvent);

                await _dbContext.SaveChangesAsync();
                // 4. Commit transaction to ensure both operations succeed or fail together
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully for expense deletion and outbox event insertion for expense {ExpenseId} with amount {Amount}", expenseId, expense.Amount);
            }
            catch
            {
                // Rollback transaction if any operation fails
                await transaction.RollbackAsync();
                _logger.LogError("Transaction rolled back due to an error during expense deletion and outbox event insertion for expense {ExpenseId} with amount {Amount}", expenseId, expense.Amount);
                throw;
            }


        }
    }
}
