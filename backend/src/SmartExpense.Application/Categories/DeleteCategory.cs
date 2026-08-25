using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Categories;

public sealed class DeleteCategory(
    ICurrentUser currentUser,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<DeleteCategoryResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return DeleteCategoryResult.Unauthenticated();
        }

        var category = await categoryRepository.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        if (category is null)
        {
            return DeleteCategoryResult.NotFound();
        }

        var isInUse = await transactionRepository.ExistsByCategoryAsync(
            id,
            userId,
            cancellationToken);

        if (isInUse)
        {
            return DeleteCategoryResult.CategoryInUse();
        }

        categoryRepository.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DeleteCategoryResult.Success();
    }
}

public enum DeleteCategoryStatus
{
    Success,
    Unauthenticated,
    NotFound,
    CategoryInUse
}

public sealed record DeleteCategoryResult(DeleteCategoryStatus Status)
{
    public static DeleteCategoryResult Success() =>
        new(DeleteCategoryStatus.Success);

    public static DeleteCategoryResult Unauthenticated() =>
        new(DeleteCategoryStatus.Unauthenticated);

    public static DeleteCategoryResult NotFound() =>
        new(DeleteCategoryStatus.NotFound);

    public static DeleteCategoryResult CategoryInUse() =>
        new(DeleteCategoryStatus.CategoryInUse);
}
