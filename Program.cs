using System.Diagnostics;
using System.Text.Json;
using MinimalAPIs.Models;
using MinimalAPIs.Repositories;
using MinimalAPIs.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IUserService, UserService>();

builder.Services.AddValidation();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var mockFilePath = Path.Combine(app.Environment.ContentRootPath, "userListMock.json");
var users = JsonSerializer.Deserialize<List<User>>(File.ReadAllText(mockFilePath), jsonOptions) ?? [];
var nextId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

var minimal = app.MapGroup("/minimal/users");

// Các API bên dưới query trực tiếp lên list in-memory, không qua Service/Repository.
minimal.MapGet("", () => Results.Ok(users))
    .WithName("GetUsers");

minimal.MapGet("/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
})
    .WithName("GetUserById");

minimal.MapPost("", (UserCreateDto dto) =>
{
    var user = new User(nextId++, dto.Name, dto.Email, dto.Age);
    users.Add(user);
    return Results.Created($"/minimal/users/{user.Id}", user);
})
    .WithName("CreateUser");

minimal.MapPut("/{id:int}", (int id, UserCreateDto dto) =>
{
    var index = users.FindIndex(u => u.Id == id);
    if (index == -1) return Results.NotFound();

    users[index] = new User(id, dto.Name, dto.Email, dto.Age);
    return Results.Ok(users[index]);
})
    .WithName("UpdateUser");

minimal.MapPatch("/{id:int}", (int id, UserPatchDto dto) =>
{
    var index = users.FindIndex(u => u.Id == id);
    if (index == -1) return Results.NotFound();

    var current = users[index];
    var updated = current with
    {
        Name = dto.Name ?? current.Name,
        Email = dto.Email ?? current.Email,
        Age = dto.Age ?? current.Age
    };
    users[index] = updated;
    return Results.Ok(updated);
})
    .WithName("PatchUser");

minimal.MapDelete("/{id:int}", (int id) =>
{
    var index = users.FindIndex(u => u.Id == id);
    if (index == -1) return Results.NotFound();

    users.RemoveAt(index);
    return Results.NoContent();
})
    .WithName("DeleteUser");

// API demo dùng DI qua tầng Service/Repository.
minimal.MapGet("/count", (int age, IUserService userService) =>
{
    var stopwatch = Stopwatch.StartNew();
    var count = userService.CountByAge(age);
    stopwatch.Stop();

    return Results.Ok(new
    {
        age,
        totalCount = count,
        elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
    });
})
    .WithName("CountUsers");

app.Run();
