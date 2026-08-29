using FinCore.Api.Contracts.Accounts;
using FinCore.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

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
        CancellationToken cancellationToken)
    {
        var accounts =
            await _accountService.GetAllAsync(
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
}