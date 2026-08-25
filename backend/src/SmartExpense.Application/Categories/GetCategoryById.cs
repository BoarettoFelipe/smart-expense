using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Categories;

public sealed class GetCategoryById(
    ICurrentUser currentUser,
    ICategoryRepository categoryRepository)
{
    public async Task<GetCategoryByIdResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return GetCategoryByIdResult.Unauthenticated();
        }

        var category = await categoryRepository.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        return category is null
            ? GetCategoryByIdResult.NotFound()
            : GetCategoryByIdResult.Success(CategoryModel.FromEntity(category));
    }
}

public enum GetCategoryByIdStatus
{
    Success,
    Unauthenticated,
    NotFound
}

public sealed record GetCategoryByIdResult(
    GetCategoryByIdStatus Status,
    CategoryModel? Category)
{
    public static GetCategoryByIdResult Success(CategoryModel category) =>
        new(GetCategoryByIdStatus.Success, category);

    public static GetCategoryByIdResult Unauthenticated() =>
        new(GetCategoryByIdStatus.Unauthenticated, null);

    public static GetCategoryByIdResult NotFound() =>
        new(GetCategoryByIdStatus.NotFound, null);
}
