namespace SmartExpense.Api.Contracts.Categories;

public sealed record CreateCategoryRequest(
    string Name,
    string Type);

public sealed record UpdateCategoryRequest(
    string Name,
    string Type);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Type,
    DateTimeOffset CreatedAt);

public sealed record CategoryErrorResponse(
    string Message,
    IReadOnlyCollection<string>? Errors = null);
