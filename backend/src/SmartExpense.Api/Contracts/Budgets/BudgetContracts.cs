namespace SmartExpense.Api.Contracts.Budgets;

public sealed record CreateBudgetRequest(
    int Month,
    int Year,
    decimal Amount);

public sealed record UpdateBudgetRequest(
    int Month,
    int Year,
    decimal Amount);

public sealed record BudgetResponse(
    Guid Id,
    int Month,
    int Year,
    decimal Amount,
    DateTimeOffset CreatedAt);

public sealed record BudgetErrorResponse(
    string Message,
    IReadOnlyCollection<string>? Errors = null);
