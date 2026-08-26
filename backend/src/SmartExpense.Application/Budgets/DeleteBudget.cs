using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Budgets;

public sealed class DeleteBudget(
    ICurrentUser currentUser,
    IBudgetRepository budgetRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<DeleteBudgetResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return DeleteBudgetResult.Unauthenticated();
        }

        var budget = await budgetRepository.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        if (budget is null)
        {
            return DeleteBudgetResult.NotFound();
        }

        budgetRepository.Remove(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DeleteBudgetResult.Success();
    }
}

public enum DeleteBudgetStatus
{
    Success,
    Unauthenticated,
    NotFound
}

public sealed record DeleteBudgetResult(DeleteBudgetStatus Status)
{
    public static DeleteBudgetResult Success() =>
        new(DeleteBudgetStatus.Success);

    public static DeleteBudgetResult Unauthenticated() =>
        new(DeleteBudgetStatus.Unauthenticated);

    public static DeleteBudgetResult NotFound() =>
        new(DeleteBudgetStatus.NotFound);
}
