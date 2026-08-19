using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartExpense.Application.Authentication;
using SmartExpense.Infrastructure;
using SmartExpense.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration, jwtOptions);
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters =
            jwtOptions.CreateTokenValidationParameters();
    });
builder.Services.AddControllers();
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

app.MapControllers();

app.Run();
