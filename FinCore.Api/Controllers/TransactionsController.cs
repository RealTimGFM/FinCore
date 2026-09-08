using FinCore.Api.Contracts.Transactions;
using FinCore.Application.Common;
using FinCore.Application.Common.Pagination;
using FinCore.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController
    : ControllerBase
{
    private readonly TransactionService _transactionService;

    public TransactionsController(
        TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var transaction =
                await _transactionService.CreateAsync(
                    new CreateTransactionCommand(
                        request.AccountId,
                        request.Amount,
                        request.Description,
                        request.OccurredAtUtc),
                    cancellationToken);

            if (transaction is null)
            {
                return NotFound(new
                {
                    error = "Account was not found."
                });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = transaction.Id },
                transaction);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                error = exception.Message
            });
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(new
            {
                error = exception.Message
            });
        }
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<TransactionDto>> Reverse(
        Guid id,
        ReverseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var reversal =
                await _transactionService.ReverseAsync(
                    new ReverseTransactionCommand(
                        id,
                        request.Description,
                        request.OccurredAtUtc),
                    cancellationToken);

            if (reversal is null)
            {
                return NotFound(new
                {
                    error = "Transaction was not found."
                });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = reversal.Id },
                reversal);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                error = exception.Message
            });
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(new
            {
                error = exception.Message
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction =
            await _transactionService.GetByIdAsync(
                id,
                cancellationToken);

        return transaction is null
            ? NotFound()
            : Ok(transaction);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetByAccount(
        [FromQuery] Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transactions =
                await _transactionService.GetByAccountIdAsync(
                    accountId,
                    page,
                    pageSize,
                    cancellationToken);

            return Ok(transactions);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }
}
