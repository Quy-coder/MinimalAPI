using Microsoft.AspNetCore.Authentication;
using MinimalAPIs.Auth;
using MinimalAPIs.Endpoints;
using MinimalAPIs.ErrorHandling;
using MinimalAPIs.Filters;
using MinimalAPIs.Repositories;
using MinimalAPIs.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<LoggingActionFilter>();
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IUserService, UserService>();

builder.Services.AddValidation();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var minimal = app.MapGroup("/minimal/users")
    .AddEndpointFilter<LoggingEndpointFilter>();

minimal.MapGet("", UserEndpoints.GetAll)
    .WithName("GetUsers");

minimal.MapGet("/count", UserEndpoints.Count)
    .WithName("CountUsers");

// Endpoint demo để trigger global exception handler.
minimal.MapGet("/boom", UserEndpoints.Boom)
    .WithName("BoomMinimal");

minimal.MapGet("/{id:int}", UserEndpoints.GetById)
    .WithName("GetUserById");

minimal.MapPost("", UserEndpoints.Create)
    .WithName("CreateUser");

minimal.MapPut("/{id:int}", UserEndpoints.Update)
    .WithName("UpdateUser");

minimal.MapPatch("/{id:int}", UserEndpoints.Patch)
    .WithName("PatchUser");

// Chỉ endpoint Delete yêu cầu auth, để so sánh trực tiếp với [Authorize] bên Controller.
minimal.MapDelete("/{id:int}", UserEndpoints.Delete)
    .WithName("DeleteUser")
    .RequireAuthorization();

app.Run();
