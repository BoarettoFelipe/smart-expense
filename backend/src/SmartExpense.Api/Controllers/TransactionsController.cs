using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartExpense.Api.Contracts.Transactions;
using SmartExpense.Application.Transactions;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(
    CreateTransaction createTransaction,
    GetTransactions getTransactions,
    GetTransactionById getTransactionById) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseTransactionType(request.Type, out var transactionType))
        {
            return BadRequest(new TransactionErrorResponse(
                "Invalid transaction type.",
                ["Type must be 'Income' or 'Expense'."]));
        }

        var result = await createTransaction.ExecuteAsync(
            new CreateTransactionCommand(
                request.Description,
                request.Amount,
                transactionType,
                request.Date,
                request.CategoryId),
            cancellationToken);

        if (result.Status == CreateTransactionStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == CreateTransactionStatus.CategoryUnavailable)
        {
            return NotFound(new TransactionErrorResponse(
                "Category was not found."));
        }

        if (result.Status == CreateTransactionStatus.Invalid)
        {
            return BadRequest(new TransactionErrorResponse(
                "Transaction is invalid.",
                result.Errors));
        }

        var response = Map(result.Transaction!);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await getTransactions.ExecuteAsync(cancellationToken);

        if (result.Status == GetTransactionsStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        return Ok(result.Transactions.Select(Map).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await getTransactionById.ExecuteAsync(
            id,
            cancellationToken);

        if (result.Status == GetTransactionByIdStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == GetTransactionByIdStatus.NotFound)
        {
            return NotFound(new TransactionErrorResponse(
                "Transaction was not found."));
        }

        return Ok(Map(result.Transaction!));
    }

    private static bool TryParseTransactionType(
        string value,
        out TransactionType transactionType)
    {
        return Enum.TryParse(value, ignoreCase: true, out transactionType) &&
               Enum.IsDefined(transactionType);
    }

    private static TransactionResponse Map(TransactionModel transaction)
    {
        return new TransactionResponse(
            transaction.Id,
            transaction.Description,
            transaction.Amount,
            transaction.Type.ToString(),
            transaction.Date,
            transaction.CategoryId,
            transaction.CreatedAt,
            transaction.UpdatedAt);
    }
}
