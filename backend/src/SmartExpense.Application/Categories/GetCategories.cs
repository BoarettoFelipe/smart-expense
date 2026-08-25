using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Categories;

public sealed class GetCategories(
    ICurrentUser currentUser,
    ICategoryRepository categoryRepository)
{
    public async Task<GetCategoriesResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return GetCategoriesResult.Unauthenticated();
        }

        var categories = await categoryRepository.GetByUserAsync(
            userId,
            cancellationToken);

        return GetCategoriesResult.Success(
            categories.Select(CategoryModel.FromEntity).ToArray());
    }
}

public enum GetCategoriesStatus
{
    Success,
    Unauthenticated
}

public sealed record GetCategoriesResult(
    GetCategoriesStatus Status,
    IReadOnlyList<CategoryModel> Categories)
{
    public static GetCategoriesResult Success(
        IReadOnlyList<CategoryModel> categories) =>
        new(GetCategoriesStatus.Success, categories);

    public static GetCategoriesResult Unauthenticated() =>
        new(GetCategoriesStatus.Unauthenticated, []);
}
