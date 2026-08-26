using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Budgets;

public sealed class UpdateBudget(
    ICurrentUser currentUser,
    IBudgetRepository budgetRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdateBudgetResult> ExecuteAsync(
        UpdateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return UpdateBudgetResult.Unauthenticated();
        }

        var budget = await budgetRepository.GetByIdAsync(
            command.Id,
            userId,
            cancellationToken);

        if (budget is null)
        {
            return UpdateBudgetResult.NotFound();
        }

        var periodBudget = await budgetRepository.GetByPeriodAsync(
            userId,
            command.Month,
            command.Year,
            cancellationToken);

        if (periodBudget is not null && periodBudget.Id != budget.Id)
        {
            return UpdateBudgetResult.PeriodConflict();
        }

        try
        {
            budget.Update(command.Month, command.Year, command.Amount);
        }
        catch (ArgumentException)
        {
            return UpdateBudgetResult.Invalid();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateBudgetResult.Success(BudgetModel.FromEntity(budget));
    }
}

public sealed record UpdateBudgetCommand(
    Guid Id,
    int Month,
    int Year,
    decimal Amount);

public enum UpdateBudgetStatus
{
    Success,
    Unauthenticated,
    NotFound,
    PeriodConflict,
    Invalid
}

public sealed record UpdateBudgetResult(
    UpdateBudgetStatus Status,
    BudgetModel? Budget,
    IReadOnlyCollection<string> Errors)
{
    public static UpdateBudgetResult Success(BudgetModel budget) =>
        new(UpdateBudgetStatus.Success, budget, []);

    public static UpdateBudgetResult Unauthenticated() =>
        new(UpdateBudgetStatus.Unauthenticated, null, []);

    public static UpdateBudgetResult NotFound() =>
        new(UpdateBudgetStatus.NotFound, null, []);

    public static UpdateBudgetResult PeriodConflict() =>
        new(UpdateBudgetStatus.PeriodConflict, null, []);

    public static UpdateBudgetResult Invalid() =>
        new(
            UpdateBudgetStatus.Invalid,
            null,
            ["Budget data is invalid."]);
}
