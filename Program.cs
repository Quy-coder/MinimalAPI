using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using MinimalAPIs.Auth;
using MinimalAPIs.Endpoints;
using MinimalAPIs.ErrorHandling;
using MinimalAPIs.Filters;
using MinimalAPIs.Models;
using MinimalAPIs.Repositories;
using MinimalAPIs.Services;
using MinimalAPIs.Validators;
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

// UserCreateDto: validated with DataAnnotations, runs automatically via AddValidation() (native .NET, both styles).
builder.Services.AddValidation();
// UserPatchDto: validated with FluentValidation via DI (registers IValidator<T>), see UsersController.Patch
// (called manually) and ValidationEndpointFilter<T> (Minimal API, see Program.cs below).
builder.Services.AddScoped<IValidator<UserPatchDto>, UserPatchDtoValidator>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

// API Versioning (package Asp.Versioning.Http + Asp.Versioning.Mvc), shared config for both styles:
// - Minimal API: app.NewApiVersionSet() + .WithApiVersionSet()/.MapToApiVersion() (Asp.Versioning.Http).
// - Controller: [ApiVersion]/[MapToApiVersion] attribute, enabled via .AddMvc() (Asp.Versioning.Mvc).
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddMvc();

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

// All route mapping for the "Users" feature lives in Endpoints/UserEndpoints.cs
// (MapUserEndpoints). Program.cs only has 1 line calling out to it; adding a new feature (Order, Product,...)
// doesn't bloat this file.
app.MapUserEndpoints();

app.Run();
