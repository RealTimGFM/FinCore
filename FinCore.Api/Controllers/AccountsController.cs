using FinCore.Api.Contracts.Accounts;
using FinCore.Application.Accounts;
using FinCore.Domain.Accounts;
using Microsoft.AspNetCore.Mvc;
using FinCore.Application.Common;

namespace FinCore.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController
    : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountsController(
        AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountService.CreateAsync(
                new CreateAccountCommand(
                    request.Name,
                    request.Type,
                    request.Currency),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = account.Id },
                account);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAll(
        [FromQuery] bool includeClosed,
        CancellationToken cancellationToken)
    {
        var accounts =
            await _accountService.GetAllAsync(
                includeClosed,
                cancellationToken);

        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account =
            await _accountService.GetByIdAsync(
                id,
                cancellationToken);

        if (account is null)
        {
            return NotFound();
        }

        return Ok(account);
    }

    [HttpPatch("{id:guid}/name")]
    public async Task<ActionResult<AccountDto>> Rename(
        Guid id,
        RenameAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountService.RenameAsync(
                id,
                request.Name,
                cancellationToken);

            return account is null
                ? NotFound()
                : Ok(account);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<AccountDto>> Close(
        Guid id,
        CloseAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountService.CloseAsync(
                id,
                request.Reason,
                AccountStatusChangeSource.User,
                cancellationToken);

            return account is null
                ? NotFound()
                : Ok(account);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<AccountDto>> Reopen(
        Guid id,
        ReopenAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountService.ReopenAsync(
                id,
                request.Reason,
                AccountStatusChangeSource.User,
                cancellationToken);

            return account is null
                ? NotFound()
                : Ok(account);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("{id:guid}/status-history")]
    public async Task<ActionResult<IReadOnlyList<AccountStatusChangeDto>>> GetStatusHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var statusHistory = await _accountService.GetStatusHistoryAsync(
            id,
            cancellationToken);

        return statusHistory is null
            ? NotFound()
            : Ok(statusHistory);
    }
}
