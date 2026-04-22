using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Events.Models;
using SpendWiselyAPI.Infrastructure.Repositories;
using System.Text.Json;
using static MongoDB.Driver.WriteConcern;

namespace SpendWiselyAPI.Application.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IBudgetRepository _budgetRepository;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<BudgetService> _logger;
        private readonly IEventStoreRepository _eventStore;
        private readonly IEventPublisher _eventPublisher;
        private readonly IOutboxEventRepository _outboxEventRepository;

        public BudgetService(IBudgetRepository budgetRepository, AppDbContext dbContext, ILogger<BudgetService> logger, 
            IEventStoreRepository eventStore, IEventPublisher eventPublisher, IOutboxEventRepository outboxEventRepository)
        {
            _budgetRepository = budgetRepository;
            _dbContext = dbContext;
            _logger = logger;
            _eventStore = eventStore;
            _eventPublisher = eventPublisher;
            _outboxEventRepository = outboxEventRepository;

        }

        public Task<Budget?> GetBudgetByIdAsync(Guid id)
            => _budgetRepository.GetBudgetByIdAsync(id);

        public Task<IEnumerable<Budget>> GetBudgetsByUserAsync(Guid userId)
            => _budgetRepository.GetBudgetsByUserAsync(userId);

        public Task<Budget?> GetBudgetByUserCategoryMonthYearAsync(
            Guid userId,
            Guid? categoryId,
            int month,
            int year)
            => _budgetRepository.GetBudgetByUserCategoryMonthYearAsync(
                userId, categoryId, month, year);

        public Task<bool> CheckBudgetExistsAsync(
            Guid userId,
            Guid? categoryId,
            int month,
            int year)
            => _budgetRepository.CheckBudgetExistsAsync(
                userId, categoryId, month, year);

        public async Task<Budget> CreateBudgetAsync(Budget budget, CancellationToken ct)
        {
            // 1. Enforce unique rule
            bool exists = await _budgetRepository.CheckBudgetExistsAsync(
                budget.UserId,
                budget.CategoryId,
                budget.Month,
                budget.Year
            );

            if (exists)
                throw new InvalidOperationException(
                    "A budget already exists for this category and month."
                );

            // 2. Create budget and outbox event within a transaction

            // Start transaction to ensure atomicity of budget creation and outbox event insertion
            _logger.LogInformation("Starting transaction for budget creation and outbox event insertion");
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Insert budget into SQL Server
                _logger.LogInformation("Inserting budget into database for user {UserId} with amount {Amount} for category {CategoryId} and month {Month} and Year {Year}", budget.UserId, budget.Amount, budget.CategoryId, budget.Month, budget.Year);
                await _budgetRepository.AddBudgetAsync(budget);

                //Create budget created event
                var budgetEvent = new BudgetEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = "BudgetCreated",
                    BudgetId = budget.Id,
                    UserId = budget.UserId,
                    Amount = budget.Amount,
                    CategoryId = budget.CategoryId,
                    Month = budget.Month,
                    Year = budget.Year,
                    Timestamp = DateTime.UtcNow
                };
                // 2. Create outbox event 
                var outboxEvent = new OutboxEvent
                {
                    Id = budgetEvent.EventId,
                    EventType = budgetEvent.EventType,
                    AggregateId = budgetEvent.BudgetId,
                    Payload = JsonSerializer.Serialize(budgetEvent),
                    Processed = false,
                    CreatedAt = budgetEvent.Timestamp
                };
                // 3. Save outbox event in the same transaction
                await _outboxEventRepository.AddOutboxEventAsync(outboxEvent);
                _logger.LogInformation("Budget created and outbox event saved for user {UserId} with amount {Amount} for category {CategoryId} and month {Month} and year {Year}", budget.UserId, budget.Amount, budget.CategoryId, budget.Month, budget.Year);
                await _budgetRepository.SaveChangesAsync(ct);
                // 4. Commit transaction to ensure both operations succeed or fail together
                await transaction.CommitAsync();
                _logger.LogInformation("Budget created and outbox event saved for user {UserId} with amount {Amount} for category {CategoryId} and month {Month} and year {Year}", budget.UserId, budget.Amount, budget.CategoryId, budget.Month, budget.Year);
            }
            catch
            {
                // Rollback transaction if any operation fails
                await transaction.RollbackAsync();
                _logger.LogError("Transaction rolled back due to an error during budget creation and outbox event insertion for user {UserId} with amount {Amount} for category {CategoryId} and month {Month} and year {Year}", budget.UserId, budget.Amount, budget.CategoryId, budget.Month, budget.Year);
                throw;
            }

            return budget;
        }

        public async Task<Budget> UpdateBudgetAsync(Budget budget, CancellationToken ct)
        {
            var existing = await _budgetRepository.GetBudgetByIdAsync(budget.Id);

            if (existing == null)
                throw new KeyNotFoundException("Budget not found.");

            // If category/month/year changed → re-check uniqueness
            bool categoryChanged = existing.CategoryId != budget.CategoryId;
            bool monthChanged = existing.Month != budget.Month;
            bool yearChanged = existing.Year != budget.Year;

            if (categoryChanged || monthChanged || yearChanged)
            {
                bool exists = await _budgetRepository.CheckBudgetExistsAsync(
                    budget.UserId,
                    budget.CategoryId,
                    budget.Month,
                    budget.Year
                );

                if (exists)
                    throw new InvalidOperationException(
                        "A budget already exists for this category and month."
                    );
            }

        

            // Start transaction to ensure atomicity of budget updation and outbox event insertion
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {

                _logger.LogInformation("Starting transaction for budget update and outbox event insertion for budget {BudgetId} with amount {Amount} for category {CategoryId} , Month {Month} , Year {Year}", budget.Id, budget.Amount, budget.CategoryId, budget.Month, budget.Year);
                //Update budget  event
                var budgetEvent = new BudgetEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = "BudgetUpdated",
                    BudgetId = budget.Id,
                    UserId = budget.UserId,
                    CategoryId = budget.CategoryId,
                    Amount = budget.Amount,
                    Month = budget.Month,
                    Year = budget.Year,
                    Timestamp = DateTime.UtcNow
                };
                //  Create outbox event 
                var outboxEvent = new OutboxEvent
                {
                    Id = budgetEvent.EventId,
                    EventType = budgetEvent.EventType,
                    AggregateId = budgetEvent.BudgetId,
                    Payload = JsonSerializer.Serialize(budgetEvent),
                    Processed = false,
                    CreatedAt = budgetEvent.Timestamp
                };
                // 1. Update budget into SQL Server
                await _budgetRepository.UpdateBudgetAsync(budget);
                // 2. Save outbox event in the same transaction
                await _outboxEventRepository.AddOutboxEventAsync(outboxEvent);

                // 3. Save changes to ensure both budget update and outbox event insertion are part of the same transaction
                await _dbContext.SaveChangesAsync(ct);
                // 4. Commit transaction to ensure both operations succeed or fail together
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully for budget update and outbox event insertion for budget {BudgetId} with amount {Amount} for category {CategoryId} , Month {Month} , Year {Year}", budget.Id, budget.Amount, budget.CategoryId, budget.Month, budget.Year);
            }
            catch
            {
                // Rollback transaction if any operation fails
                await transaction.RollbackAsync();
                throw;
            }



            return budget;
        }

        public async Task DeleteBudgetAsync(Guid budgetId, CancellationToken ct)
        {
            var existing = await _budgetRepository.GetBudgetByIdAsync(budgetId);

            if (existing == null)
                throw new KeyNotFoundException("Budget not found.");

            

            // Start transaction to ensure atomicity of budget deletion and outbox event insertion
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Delete budget from SQL Server
                _logger.LogInformation("Starting transaction for budget deletion and outbox event insertion for budget {BudgetId}", budgetId);
                await _budgetRepository.DeleteBudgetAsync(existing);

                //Delete budget  event
                var budgetEvent = new BudgetEvent
                {
                    EventId = Guid.NewGuid(),
                    BudgetId = budgetId,
                    EventType = "BudgetDeleted",
                    UserId = existing.UserId,
                    Amount = existing.Amount,
                    Month = existing.Month,
                    CategoryId = existing.CategoryId,
                    Year = existing.Year,
                    Timestamp = DateTime.UtcNow
                };
                // 2. Create outbox event 
                var outboxEvent = new OutboxEvent
                {
                    Id = budgetEvent.EventId,
                    EventType = budgetEvent.EventType,
                    AggregateId = budgetEvent.BudgetId,
                    Payload = JsonSerializer.Serialize(budgetEvent),
                    Processed = false,
                    CreatedAt = budgetEvent.Timestamp
                };
                // 3. Save outbox event in the same transaction
                await _outboxEventRepository.AddOutboxEventAsync(outboxEvent);

                await _dbContext.SaveChangesAsync(ct);
                // 4. Commit transaction to ensure both operations succeed or fail together
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully for budget deletion and outbox event insertion for budget {BudgetId}", budgetId);
            }
            catch
            {
                // Rollback transaction if any operation fails
                await transaction.RollbackAsync();
                _logger.LogError("Transaction rolled back due to an error during budget deletion and outbox event insertion for budget {BudgetId} ", budgetId);
                throw;
            }

        }
    }
}
