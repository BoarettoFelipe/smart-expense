using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartExpense.Api.Authentication;
using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Authentication;
using SmartExpense.Application.Budgets;
using SmartExpense.Application.Categories;
using SmartExpense.Application.Dashboard;
using SmartExpense.Application.Transactions;
using SmartExpense.Infrastructure;
using SmartExpense.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration, jwtOptions);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<CreateTransaction>();
builder.Services.AddScoped<GetTransactions>();
builder.Services.AddScoped<GetTransactionById>();
builder.Services.AddScoped<UpdateTransaction>();
builder.Services.AddScoped<DeleteTransaction>();
builder.Services.AddScoped<CreateCategory>();
builder.Services.AddScoped<GetCategories>();
builder.Services.AddScoped<GetCategoryById>();
builder.Services.AddScoped<UpdateCategory>();
builder.Services.AddScoped<DeleteCategory>();
builder.Services.AddScoped<CreateBudget>();
builder.Services.AddScoped<GetBudgets>();
builder.Services.AddScoped<GetBudgetById>();
builder.Services.AddScoped<UpdateBudget>();
builder.Services.AddScoped<DeleteBudget>();
builder.Services.AddScoped<GetDashboard>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters =
            jwtOptions.CreateTokenValidationParameters();
    });
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
