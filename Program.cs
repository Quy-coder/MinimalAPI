using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

app.UseAuthorization();

app.MapControllers();

app.MapGet("/users", () =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "userListMock.json");
    var json = File.ReadAllText(filePath);
    var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });
    return Results.Ok(users);
})
.WithName("GetUsers");

app.Run();

record User(int Id, string Name, string Email, int Age);
