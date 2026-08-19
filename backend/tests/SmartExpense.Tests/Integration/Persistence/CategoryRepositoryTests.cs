using Microsoft.EntityFrameworkCore;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;
using SmartExpense.Infrastructure.Persistence.Repositories;

namespace SmartExpense.Tests.Integration.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class CategoryRepositoryTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_StagesCategoryUntilUnitOfWorkSavesIt()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);

        await using var dbContext = fixture.CreateDbContext();
        var repository = new CategoryRepository(dbContext);
        IUnitOfWork unitOfWork = dbContext;

        Assert.Same(dbContext, unitOfWork);
        Assert.True(fixture.MigrationsApplied);

        await repository.AddAsync(category);

        await using (var beforeSaveContext = fixture.CreateDbContext())
        {
            Assert.False(await beforeSaveContext.Categories.AnyAsync(
                item => item.Id == category.Id));
        }

        Assert.Equal(1, await unitOfWork.SaveChangesAsync());

        await using var verificationContext = fixture.CreateDbContext();
        Assert.NotNull(await new CategoryRepository(verificationContext)
            .GetByIdAsync(category.Id, userId));
    }

    [Fact]
    public async Task GetByIdAsync_WithMatchingIdAndUserId_ReturnsCategory()
    {
        var userId = Guid.NewGuid();
        var category = await PersistCategoryAsync(userId);

        await using var dbContext = fixture.CreateDbContext();
        var result = await new CategoryRepository(dbContext)
            .GetByIdAsync(category.Id, userId);

        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentUserId_ReturnsNull()
    {
        var category = await PersistCategoryAsync(Guid.NewGuid());

        await using var dbContext = fixture.CreateDbContext();
        var result = await new CategoryRepository(dbContext)
            .GetByIdAsync(category.Id, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyRequestedUsersCategories()
    {
        var requestedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var requestedCategory = await PersistCategoryAsync(requestedUserId);
        await PersistCategoryAsync(otherUserId);

        await using var dbContext = fixture.CreateDbContext();
        var results = await new CategoryRepository(dbContext)
            .GetByUserAsync(requestedUserId);

        var result = Assert.Single(results);
        Assert.Equal(requestedCategory.Id, result.Id);
        Assert.Equal(requestedUserId, result.UserId);
    }

    private async Task<Category> PersistCategoryAsync(Guid userId)
    {
        await using var dbContext = fixture.CreateDbContext();
        var category = CreateCategory(userId);

        await new CategoryRepository(dbContext).AddAsync(category);
        Assert.Equal(1, await ((IUnitOfWork)dbContext).SaveChangesAsync());

        return category;
    }

    private static Category CreateCategory(Guid userId)
    {
        return new Category(
            Guid.NewGuid(),
            $"Category-{Guid.NewGuid():N}",
            TransactionType.Income,
            userId,
            CreatedAt);
    }
}
