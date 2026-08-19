using Microsoft.EntityFrameworkCore;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;

namespace SmartExpense.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(SmartExpenseDbContext dbContext)
    : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Categories.SingleOrDefaultAsync(
            category => category.Id == id && category.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Categories.AddAsync(category, cancellationToken);
    }

    public void Remove(Category category)
    {
        dbContext.Categories.Remove(category);
    }
}
