using FinCore.Api.Contracts.Transactions;
using FinCore.Application.Common;
using FinCore.Application.Common.Pagination;
using FinCore.Application.Transactions;
using FinCore.Infrastructure.Idempotency;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController
    : ControllerBase
{
    private readonly IdempotencyService _idempotencyService;
    private readonly TransactionService _transactionService;

    public TransactionsController(
        IdempotencyService idempotencyService,
        TransactionService transactionService)
    {
        _idempotencyService = idempotencyService;
        _transactionService = transactionService;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(
        CreateTransactionRequest request,
        [FromHeader(Name = "Idempotency-Key")]
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new
            {
                error = "Idempotency-Key header is required."
            });
        }

        try
        {
            var result =
                await _idempotencyService.ExecuteAsync<
                    CreateTransactionRequest,
                    TransactionDto>(
                    idempotencyKey,
                    request,
                    async () =>
                        await _transactionService.CreateAsync(
                            new CreateTransactionCommand(
                                request.AccountId,
                                request.Amount,
                                request.Description,
                                request.OccurredAtUtc),
                            cancellationToken)
                        ?? throw new KeyNotFoundException(
                            "Account was not found."),
                    StatusCodes.Status201Created,
                    cancellationToken);

            return StatusCode(
                result.StatusCode,
                result.Value);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                error = exception.Message
            });
        }
        catch (IdempotencyKeyReuseException exception)
        {
            return Conflict(new
            {
                error = exception.Message
            });
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

    [HttpPut("{id:guid}/category")]
    public async Task<ActionResult<TransactionDto>> SetCategory(
        Guid id,
        SetTransactionCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var transaction =
                await _transactionService.SetCategoryAsync(
                    id,
                    request.CategoryId,
                    cancellationToken);

            return transaction is null
                ? NotFound(new
                {
                    error = "Transaction was not found."
                })
                : Ok(transaction);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
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
