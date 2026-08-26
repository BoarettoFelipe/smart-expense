using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Budgets;

public sealed class GetBudgets(
    ICurrentUser currentUser,
    IBudgetRepository budgetRepository)
{
    public async Task<GetBudgetsResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return GetBudgetsResult.Unauthenticated();
        }

        var budgets = await budgetRepository.GetByUserAsync(
            userId,
            cancellationToken);

        return GetBudgetsResult.Success(
            budgets.Select(BudgetModel.FromEntity).ToArray());
    }
}

public enum GetBudgetsStatus
{
    Success,
    Unauthenticated
}

public sealed record GetBudgetsResult(
    GetBudgetsStatus Status,
    IReadOnlyList<BudgetModel> Budgets)
{
    public static GetBudgetsResult Success(IReadOnlyList<BudgetModel> budgets) =>
        new(GetBudgetsStatus.Success, budgets);

    public static GetBudgetsResult Unauthenticated() =>
        new(GetBudgetsStatus.Unauthenticated, []);
}
