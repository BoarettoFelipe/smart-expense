using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartExpense.Api.Contracts.Categories;
using SmartExpense.Application.Categories;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(
    CreateCategory createCategory,
    GetCategories getCategories,
    GetCategoryById getCategoryById,
    UpdateCategory updateCategory,
    DeleteCategory deleteCategory) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseTransactionType(request.Type, out var transactionType))
        {
            return InvalidTransactionType();
        }

        var result = await createCategory.ExecuteAsync(
            new CreateCategoryCommand(request.Name, transactionType),
            cancellationToken);

        if (result.Status == CreateCategoryStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == CreateCategoryStatus.Invalid)
        {
            return BadRequest(new CategoryErrorResponse(
                "Category is invalid.",
                result.Errors));
        }

        var response = Map(result.Category!);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await getCategories.ExecuteAsync(cancellationToken);

        if (result.Status == GetCategoriesStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        return Ok(result.Categories.Select(Map).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await getCategoryById.ExecuteAsync(id, cancellationToken);

        if (result.Status == GetCategoryByIdStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == GetCategoryByIdStatus.NotFound)
        {
            return NotFound(new CategoryErrorResponse(
                "Category was not found."));
        }

        return Ok(Map(result.Category!));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseTransactionType(request.Type, out var transactionType))
        {
            return InvalidTransactionType();
        }

        var result = await updateCategory.ExecuteAsync(
            new UpdateCategoryCommand(id, request.Name, transactionType),
            cancellationToken);

        if (result.Status == UpdateCategoryStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == UpdateCategoryStatus.NotFound)
        {
            return NotFound(new CategoryErrorResponse(
                "Category was not found."));
        }

        if (result.Status == UpdateCategoryStatus.Invalid)
        {
            return BadRequest(new CategoryErrorResponse(
                "Category is invalid.",
                result.Errors));
        }

        return Ok(Map(result.Category!));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await deleteCategory.ExecuteAsync(id, cancellationToken);

        if (result.Status == DeleteCategoryStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == DeleteCategoryStatus.NotFound)
        {
            return NotFound(new CategoryErrorResponse(
                "Category was not found."));
        }

        if (result.Status == DeleteCategoryStatus.CategoryInUse)
        {
            return Conflict(new CategoryErrorResponse(
                "Category cannot be deleted because it is used by one or more transactions."));
        }

        return NoContent();
    }

    private BadRequestObjectResult InvalidTransactionType()
    {
        return BadRequest(new CategoryErrorResponse(
            "Invalid transaction type.",
            ["Type must be 'Income' or 'Expense'."]));
    }

    private static bool TryParseTransactionType(
        string value,
        out TransactionType transactionType)
    {
        return Enum.TryParse(value, ignoreCase: true, out transactionType) &&
               Enum.IsDefined(transactionType);
    }

    private static CategoryResponse Map(CategoryModel category)
    {
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.Type.ToString(),
            category.CreatedAt);
    }
}
