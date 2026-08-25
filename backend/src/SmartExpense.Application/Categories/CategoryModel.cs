using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Application.Categories;

public sealed record CategoryModel(
    Guid Id,
    string Name,
    TransactionType Type,
    DateTimeOffset CreatedAt)
{
    internal static CategoryModel FromEntity(Category category)
    {
        return new CategoryModel(
            category.Id,
            category.Name,
            category.Type,
            category.CreatedAt);
    }
}
