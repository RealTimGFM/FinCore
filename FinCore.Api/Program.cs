using FinCore.Application.Accounts;
using FinCore.Application.Categories;
using FinCore.Application.Common.Persistence;
using FinCore.Application.Transactions;
using FinCore.Infrastructure.Accounts;
using FinCore.Infrastructure.Categories;
using FinCore.Infrastructure.Idempotency;
using FinCore.Infrastructure.Persistence;
using FinCore.Infrastructure.Transactions;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    }); 
builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<FinCoreDbContext>(
    options =>
    {
        options.UseSqlServer(connectionString);
    });

builder.Services.AddScoped<
    IAccountRepository,
    AccountRepository>();

builder.Services.AddScoped<
    ITransactionRepository,
    TransactionRepository>();

builder.Services.AddScoped<
    ICategoryRepository,
    CategoryRepository>();

builder.Services.AddScoped<
    IUnitOfWork,
    EfUnitOfWork>();

builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<IdempotencyService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
