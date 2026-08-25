using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Application.Categories;

public sealed class UpdateCategory(
    ICurrentUser currentUser,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdateCategoryResult> ExecuteAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return UpdateCategoryResult.Unauthenticated();
        }

        var category = await categoryRepository.GetByIdAsync(
            command.Id,
            userId,
            cancellationToken);

        if (category is null)
        {
            return UpdateCategoryResult.NotFound();
        }

        try
        {
            category.Update(command.Name, command.Type);
        }
        catch (ArgumentException)
        {
            return UpdateCategoryResult.Invalid();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateCategoryResult.Success(CategoryModel.FromEntity(category));
    }
}

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    TransactionType Type);

public enum UpdateCategoryStatus
{
    Success,
    Unauthenticated,
    NotFound,
    Invalid
}

public sealed record UpdateCategoryResult(
    UpdateCategoryStatus Status,
    CategoryModel? Category,
    IReadOnlyCollection<string> Errors)
{
    public static UpdateCategoryResult Success(CategoryModel category) =>
        new(UpdateCategoryStatus.Success, category, []);

    public static UpdateCategoryResult Unauthenticated() =>
        new(UpdateCategoryStatus.Unauthenticated, null, []);

    public static UpdateCategoryResult NotFound() =>
        new(UpdateCategoryStatus.NotFound, null, []);

    public static UpdateCategoryResult Invalid() =>
        new(
            UpdateCategoryStatus.Invalid,
            null,
            ["Category data is invalid."]);
}
