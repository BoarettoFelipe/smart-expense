namespace SmartExpense.Api.Contracts.Transactions;

public sealed record CreateTransactionRequest(
    string Description,
    decimal Amount,
    string Type,
    DateOnly Date,
    Guid CategoryId);

public sealed record UpdateTransactionRequest(
    string Description,
    decimal Amount,
    string Type,
    DateOnly Date,
    Guid CategoryId);

public sealed record TransactionResponse(
    Guid Id,
    string Description,
    decimal Amount,
    string Type,
    DateOnly Date,
    Guid CategoryId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record TransactionErrorResponse(
    string Message,
    IReadOnlyCollection<string>? Errors = null);
