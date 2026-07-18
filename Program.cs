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

// Version set dùng chung cho cả group: khai báo version nào tồn tại, endpoint không gọi
// .MapToApiVersion(...) mặc định phục vụ mọi version trong set (giống Controller).
var apiVersionSet = app.NewApiVersionSet("Users (Minimal)")
    .HasApiVersion(new ApiVersion(1.0))
    .HasApiVersion(new ApiVersion(2.0))
    .ReportApiVersions()
    .Build();

var minimal = app.MapGroup("/minimal/v{version:apiVersion}/users")
    .WithApiVersionSet(apiVersionSet)
    .AddEndpointFilter<LoggingEndpointFilter>();

minimal.MapGet("", UserEndpoints.GetAll)
    .WithName("GetUsers")
    .MapToApiVersion(1.0);

// v2.0 của GetAll, cùng route/verb với bản v1 -> bắt buộc .MapToApiVersion để phân biệt.
minimal.MapGet("", UserEndpoints.GetAllV2)
    .WithName("GetUsersV2")
    .MapToApiVersion(2.0);

minimal.MapGet("/search", UserEndpoints.Search)
    .WithName("SearchUsers");

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

// FluentValidation qua IEndpointFilter: validate UserPatchDto trước khi vào handler.
minimal.MapPatch("/{id:int}", UserEndpoints.Patch)
    .WithName("PatchUser")
    .AddEndpointFilter<ValidationEndpointFilter<UserPatchDto>>();

// Chỉ endpoint Delete yêu cầu auth, để so sánh trực tiếp với [Authorize] bên Controller.
minimal.MapDelete("/{id:int}", UserEndpoints.Delete)
    .WithName("DeleteUser")
    .RequireAuthorization();

app.Run();
