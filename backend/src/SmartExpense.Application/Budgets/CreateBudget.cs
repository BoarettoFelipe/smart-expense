using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;

namespace SmartExpense.Application.Budgets;

public sealed class CreateBudget(
    ICurrentUser currentUser,
    IBudgetRepository budgetRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<CreateBudgetResult> ExecuteAsync(
        CreateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return CreateBudgetResult.Unauthenticated();
        }

        var existingBudget = await budgetRepository.GetByPeriodAsync(
            userId,
            command.Month,
            command.Year,
            cancellationToken);

        if (existingBudget is not null)
        {
            return CreateBudgetResult.PeriodConflict();
        }

        Budget budget;

        try
        {
            budget = new Budget(
                Guid.NewGuid(),
                command.Month,
                command.Year,
                command.Amount,
                userId,
                DateTimeOffset.UtcNow);
        }
        catch (ArgumentException)
        {
            return CreateBudgetResult.Invalid();
        }

        await budgetRepository.AddAsync(budget, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateBudgetResult.Success(BudgetModel.FromEntity(budget));
    }
}

public sealed record CreateBudgetCommand(
    int Month,
    int Year,
    decimal Amount);

public enum CreateBudgetStatus
{
    Success,
    Unauthenticated,
    PeriodConflict,
    Invalid
}

public sealed record CreateBudgetResult(
    CreateBudgetStatus Status,
    BudgetModel? Budget,
    IReadOnlyCollection<string> Errors)
{
    public static CreateBudgetResult Success(BudgetModel budget) =>
        new(CreateBudgetStatus.Success, budget, []);

    public static CreateBudgetResult Unauthenticated() =>
        new(CreateBudgetStatus.Unauthenticated, null, []);

    public static CreateBudgetResult PeriodConflict() =>
        new(CreateBudgetStatus.PeriodConflict, null, []);

    public static CreateBudgetResult Invalid() =>
        new(
            CreateBudgetStatus.Invalid,
            null,
            ["Budget data is invalid."]);
}
