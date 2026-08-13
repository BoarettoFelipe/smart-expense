using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartExpense.Domain.Entities;

namespace SmartExpense.Infrastructure.Persistence.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(budget => budget.Id);

        builder.Property(budget => budget.Id)
            .HasColumnName("id");

        builder.Property(budget => budget.Month)
            .HasColumnName("month")
            .IsRequired();

        builder.Property(budget => budget.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(budget => budget.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(budget => budget.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(budget => budget.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
