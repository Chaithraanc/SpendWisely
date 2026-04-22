using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Events.Models;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.DbContext
{
    // Infrastructure/AppDbContext.cs
    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ExpenseEntity> Expenses { get; set; }
        public DbSet<CategoryEntity> Categories { get; set; }

        public DbSet<UserEntity> Users { get; set; }

        public DbSet<BudgetEntity> Budgets { get; set; }

        public DbSet<OutboxEvent> OutboxEvents { get; set; }

        public DbSet<OutboxFailedEvent> OutboxFailedEvents { get; set; }

        public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

        public DbSet<DashboardMonthlySummaryEntity> DashboardMonthlySummary { get; set; }

        public DbSet<AIInsightsEntity> AIInsights { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure ExpenseEntity
            modelBuilder.Entity<ExpenseEntity>(entity =>
            {
                entity.ToTable("Expenses");

                // Primary Key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.Amount)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();

                entity.Property(e => e.Description)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(e => e.UserId)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                      .IsRequired(false);

                // Relationship 
                entity.HasOne(e => e.Category)//One Expense has one Category
                      .WithMany(c => c.Expenses)//One Category can have many Expenses
                      .HasForeignKey(e => e.CategoryId)//Use CategoryId column in Expense table as FK
                      .OnDelete(DeleteBehavior.SetNull);//If a Category is deleted → set CategoryId = NULL in Expense

                entity.HasOne(e => e.User)//One Expense has one User
                      .WithMany(u => u.Expenses)//One User can have many Expenses
                      .HasForeignKey(e => e.UserId)//Use UserId column in Expense table as FK
                      .OnDelete(DeleteBehavior.Restrict);//If a User is deleted → prevent deletion if there are related Expenses




                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => new { e.UserId, e.CategoryId });

            });
            // Configure CategoryEntity
            modelBuilder.Entity<CategoryEntity>(entity =>
            {
                entity.ToTable("Categories");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.UserId)
                      .IsRequired(false);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(e => e.UpdatedAt)
                     .IsRequired(false);



                entity.HasOne(e => e.User)//One Category has one User
                      .WithMany(u => u.Categories) // one user can have many categories
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            // Configure UserEntity
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .ValueGeneratedNever(); // GUID from domain

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(e => e.Email)
                      .IsUnique();

                entity.Property(e => e.PasswordHash)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Role)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(x => x.RefreshToken)
                      .HasMaxLength(500)
                      .IsRequired(false);

                entity.Property(x => x.RefreshTokenExpiry)
                    .IsRequired(false);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(e => e.UpdatedAt);
            });

            // Configure OutboxEvent

            modelBuilder.Entity<OutboxEvent>(entity =>
            {
                entity.ToTable("OutboxEvents");

                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.EventType)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.AggregateId)
                      .IsRequired();

                entity.Property(e => e.Payload)
                      .IsRequired();

                entity.Property(e => e.Processed)
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => new { e.Processed, e.CreatedAt })
                      .HasDatabaseName("IX_OutboxEvents_Processed_CreatedAt");

                entity.HasIndex(e => e.AggregateId)
                      .HasDatabaseName("IX_OutboxEvents_AggregateId");

                entity.HasIndex(e => e.EventType)
                      .HasDatabaseName("IX_OutboxEvents_EventType");
            });

            // Configure OutboxFailedEvent
            modelBuilder.Entity<OutboxFailedEvent>(entity =>
            {
                entity.ToTable("OutboxFailedEvents");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedNever(); // GUID provided by app

                entity.Property(e => e.EventType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.AggregateId)
                    .IsRequired();

                entity.Property(e => e.Payload)
                    .IsRequired();

                entity.Property(e => e.RetryCount)
                    .HasDefaultValue(0);

                entity.Property(e => e.FailureReason)
                    .IsRequired();

                entity.Property(e => e.FailedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // indexes
                entity.HasIndex(e => e.FailedAt);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.AggregateId);
                entity.HasIndex(e => new { e.EventType, e.FailedAt });
            });

            // Configure ProcessedEvent
            modelBuilder.Entity<ProcessedEvent>(entity =>
            {
                entity.ToTable("ProcessedEvents");

                // Composite PK
                entity.HasKey(e => new { e.EventId, e.EventType });

                entity.Property(e => e.EventId)
                    .IsRequired();

                entity.Property(e => e.AggregateId)
                    .ValueGeneratedNever(); // GUID provided by app


                entity.Property(e => e.EventType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.AggregateId)
                    .IsRequired(false);

                entity.Property(e => e.UserId)
                    .IsRequired(false);

                entity.Property(e => e.Payload)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.Timestamp)
                    .HasDefaultValueSql("sysutcdatetime()")
                    .ValueGeneratedOnAdd();

            });

            // Configure BudgetEntity
            modelBuilder.Entity<BudgetEntity>(entity =>
            {
                entity.ToTable("Budgets");


                // Table
                entity.ToTable("Budgets");

                // Primary Key
                entity.HasKey(b => b.Id);

                // Domain generates GUID → EF should NOT auto-generate
                entity.Property(b => b.Id)
                    .ValueGeneratedNever();

                // UserId (Required)
                entity.Property(b => b.UserId)
                    .IsRequired();

                // CategoryId (Nullable)
                entity.Property(b => b.CategoryId)
                    .IsRequired(false);

                // Amount
                entity.Property(b => b.Amount)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                // Month
                entity.Property(b => b.Month)
                    .IsRequired();

                // Year
                entity.Property(b => b.Year)
                    .IsRequired();

                // CreatedAt
                entity.Property(b => b.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();

                // UpdatedAt
                entity.Property(b => b.UpdatedAt)
                    .ValueGeneratedOnUpdate();

               
                // Unique Index (UserId + CategoryId + Month + Year)
                entity.HasIndex(b => new { b.UserId, b.CategoryId, b.Month, b.Year })
                    .IsUnique()
                    .HasDatabaseName("IX_Budget_UserCategoryMonthYear");


            });

            // Configure DashboardMonthlySummaryEntity
            modelBuilder.Entity<DashboardMonthlySummaryEntity>(entity =>
            {
                entity.ToTable("DashboardMonthlySummary");
                // Primary Key
                entity.HasKey(d => d.Id);
                // Domain generates GUID → EF should NOT auto-generate
                entity.Property(d => d.Id)
                    .ValueGeneratedNever();
                // UserId (Required)
                entity.Property(d => d.UserId)
                    .IsRequired();
                // CategoryId (Nullable)
                entity.Property(d => d.CategoryId)
                    .IsRequired(false);
                // Month
                entity.Property(d => d.Month)
                    .IsRequired();
                // Year
                entity.Property(d => d.Year)
                    .IsRequired();
                // TotalSpent
                entity.Property(d => d.TotalSpent)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();
                // CreatedAt
                entity.Property(d => d.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();
                // UpdatedAt
                entity.Property(d => d.UpdatedAt)
                    .ValueGeneratedOnUpdate();
                // Unique Index (UserId + CategoryId + Month + Year)
                entity.HasIndex(d => new { d.UserId, d.CategoryId, d.Month, d.Year })
                    .IsUnique()
                    .HasDatabaseName("IX_Dashboard_UserCategoryYearMonth_Covering");
            });


            modelBuilder.Entity<AIInsightsEntity>(entity =>
            {
                entity.ToTable("AIInsights");


                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .IsRequired();

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.Month)
                    .IsRequired();

                entity.Property(x => x.Year)
                    .IsRequired();

                entity.Property(x => x.Insights)
                    .HasColumnName("Insights")
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("sysutcdatetime()")
                    .IsRequired();

                entity.Property(x => x.UpdatedAt)
                    .IsRequired(false);

                entity.HasOne<User>() // if you have User entity
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
