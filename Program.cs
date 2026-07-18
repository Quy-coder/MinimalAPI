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

// UserCreateDto: validate bằng DataAnnotations, tự chạy qua AddValidation() (native .NET, cả 2 style).
builder.Services.AddValidation();
// UserPatchDto: validate bằng FluentValidation qua DI (đăng ký IValidator<T>), xem UsersController.Patch
// (gọi thủ công) và ValidationEndpointFilter<T> (Minimal API, xem Program.cs bên dưới).
builder.Services.AddScoped<IValidator<UserPatchDto>, UserPatchDtoValidator>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

// API Versioning (package Asp.Versioning.Http + Asp.Versioning.Mvc), dùng chung config cho cả 2 style:
// - Minimal API: app.NewApiVersionSet() + .WithApiVersionSet()/.MapToApiVersion() (Asp.Versioning.Http).
// - Controller: [ApiVersion]/[MapToApiVersion] attribute, bật qua .AddMvc() (Asp.Versioning.Mvc).
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

// Toàn bộ route mapping của feature "Users" nằm trong Endpoints/UserEndpoints.cs
// (MapUserEndpoints). Program.cs chỉ còn 1 dòng gọi ra, thêm feature mới (Order, Product,...)
// không làm file này phình to.
app.MapUserEndpoints();

app.Run();
