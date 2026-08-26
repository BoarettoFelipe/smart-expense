using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartExpense.Api.Contracts.Budgets;
using SmartExpense.Application.Budgets;

namespace SmartExpense.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/budgets")]
public sealed class BudgetsController(
    CreateBudget createBudget,
    GetBudgets getBudgets,
    GetBudgetById getBudgetById,
    UpdateBudget updateBudget,
    DeleteBudget deleteBudget) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createBudget.ExecuteAsync(
            new CreateBudgetCommand(
                request.Month,
                request.Year,
                request.Amount),
            cancellationToken);

        if (result.Status == CreateBudgetStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == CreateBudgetStatus.PeriodConflict)
        {
            return Conflict(PeriodConflictError());
        }

        if (result.Status == CreateBudgetStatus.Invalid)
        {
            return BadRequest(new BudgetErrorResponse(
                "Budget is invalid.",
                result.Errors));
        }

        var response = Map(result.Budget!);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await getBudgets.ExecuteAsync(cancellationToken);

        if (result.Status == GetBudgetsStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        return Ok(result.Budgets.Select(Map).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await getBudgetById.ExecuteAsync(id, cancellationToken);

        if (result.Status == GetBudgetByIdStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == GetBudgetByIdStatus.NotFound)
        {
            return NotFound(new BudgetErrorResponse(
                "Budget was not found."));
        }

        return Ok(Map(result.Budget!));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateBudget.ExecuteAsync(
            new UpdateBudgetCommand(
                id,
                request.Month,
                request.Year,
                request.Amount),
            cancellationToken);

        if (result.Status == UpdateBudgetStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == UpdateBudgetStatus.NotFound)
        {
            return NotFound(new BudgetErrorResponse(
                "Budget was not found."));
        }

        if (result.Status == UpdateBudgetStatus.PeriodConflict)
        {
            return Conflict(PeriodConflictError());
        }

        if (result.Status == UpdateBudgetStatus.Invalid)
        {
            return BadRequest(new BudgetErrorResponse(
                "Budget is invalid.",
                result.Errors));
        }

        return Ok(Map(result.Budget!));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await deleteBudget.ExecuteAsync(id, cancellationToken);

        if (result.Status == DeleteBudgetStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == DeleteBudgetStatus.NotFound)
        {
            return NotFound(new BudgetErrorResponse(
                "Budget was not found."));
        }

        return NoContent();
    }

    private static BudgetErrorResponse PeriodConflictError()
    {
        return new BudgetErrorResponse(
            "A budget already exists for this month and year.");
    }

    private static BudgetResponse Map(BudgetModel budget)
    {
        return new BudgetResponse(
            budget.Id,
            budget.Month,
            budget.Year,
            budget.Amount,
            budget.CreatedAt);
    }
}
