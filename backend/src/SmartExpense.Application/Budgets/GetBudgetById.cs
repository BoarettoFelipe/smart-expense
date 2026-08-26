using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Budgets;

public sealed class GetBudgetById(
    ICurrentUser currentUser,
    IBudgetRepository budgetRepository)
{
    public async Task<GetBudgetByIdResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return GetBudgetByIdResult.Unauthenticated();
        }

        var budget = await budgetRepository.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        return budget is null
            ? GetBudgetByIdResult.NotFound()
            : GetBudgetByIdResult.Success(BudgetModel.FromEntity(budget));
    }
}

public enum GetBudgetByIdStatus
{
    Success,
    Unauthenticated,
    NotFound
}

public sealed record GetBudgetByIdResult(
    GetBudgetByIdStatus Status,
    BudgetModel? Budget)
{
    public static GetBudgetByIdResult Success(BudgetModel budget) =>
        new(GetBudgetByIdStatus.Success, budget);

    public static GetBudgetByIdResult Unauthenticated() =>
        new(GetBudgetByIdStatus.Unauthenticated, null);

    public static GetBudgetByIdResult NotFound() =>
        new(GetBudgetByIdStatus.NotFound, null);
}
