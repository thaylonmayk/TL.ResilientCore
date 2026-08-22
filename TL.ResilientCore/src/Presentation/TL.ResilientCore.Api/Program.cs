using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using TL.ResilientCore.Application;
using TL.ResilientCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.Audience = builder.Configuration["Identity:Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { Status = "TL.ResilientCore API is running!" }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.MapGet("/secure-data", (System.Security.Claims.ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        Message = "Acesso autorizado com sucesso pelo TL.IdentityHub!",
        User = user.Identity?.Name,
        Claims = user.Claims.Select(c => new { c.Type, c.Value })
    });
}).RequireAuthorization();   

app.Run();

public partial class Program { }