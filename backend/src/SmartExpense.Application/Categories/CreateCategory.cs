using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Application.Categories;

public sealed class CreateCategory(
    ICurrentUser currentUser,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<CreateCategoryResult> ExecuteAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return CreateCategoryResult.Unauthenticated();
        }

        Category category;

        try
        {
            category = new Category(
                Guid.NewGuid(),
                command.Name,
                command.Type,
                userId,
                DateTimeOffset.UtcNow);
        }
        catch (ArgumentException)
        {
            return CreateCategoryResult.Invalid();
        }

        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateCategoryResult.Success(CategoryModel.FromEntity(category));
    }
}

public sealed record CreateCategoryCommand(
    string Name,
    TransactionType Type);

public enum CreateCategoryStatus
{
    Success,
    Unauthenticated,
    Invalid
}

public sealed record CreateCategoryResult(
    CreateCategoryStatus Status,
    CategoryModel? Category,
    IReadOnlyCollection<string> Errors)
{
    public static CreateCategoryResult Success(CategoryModel category) =>
        new(CreateCategoryStatus.Success, category, []);

    public static CreateCategoryResult Unauthenticated() =>
        new(CreateCategoryStatus.Unauthenticated, null, []);

    public static CreateCategoryResult Invalid() =>
        new(
            CreateCategoryStatus.Invalid,
            null,
            ["Category data is invalid."]);
}
