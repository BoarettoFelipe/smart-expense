using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartExpense.Domain.Entities;

namespace SmartExpense.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id");

        builder.Property(transaction => transaction.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(transaction => transaction.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(transaction => transaction.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(transaction => transaction.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(transaction => transaction.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(transaction => transaction.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
