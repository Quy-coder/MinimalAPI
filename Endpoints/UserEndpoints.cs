using System.Diagnostics;
using Asp.Versioning;
using MinimalAPIs.Filters;
using MinimalAPIs.Models;
using MinimalAPIs.Services;

namespace MinimalAPIs.Endpoints;

// Handlers are extracted into static methods (instead of inline lambdas in Program.cs) so they can be
// unit-tested like ordinary functions, without needing a host app or building a ControllerContext.
//
// Route mapping is also grouped here into 1 extension method (MapUserEndpoints), instead of leaving
// individual .MapGet/.MapPost calls scattered in Program.cs. Adding a new feature (Order, Product,...)
// only requires adding a similar OrderEndpoints class + 1 app.MapOrderEndpoints() line in Program.cs,
// so Program.cs doesn't bloat as the number of endpoints grows.
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Dedicated version set for the Users group: declares which versions exist; an endpoint that doesn't call
        // .MapToApiVersion(...) serves every version in the set by default (same as Controller).
        var apiVersionSet = app.NewApiVersionSet("Users (Minimal)")
            .HasApiVersion(new ApiVersion(1.0))
            .HasApiVersion(new ApiVersion(2.0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/minimal/v{version:apiVersion}/users")
            .WithApiVersionSet(apiVersionSet)
            .AddEndpointFilter<LoggingEndpointFilter>();

        group.MapGet("", GetAll)
            .WithName("GetUsers")
            .MapToApiVersion(1.0);

        // v2.0 of GetAll, same route/verb as the v1 version -> .MapToApiVersion is required to distinguish them.
        group.MapGet("", GetAllV2)
            .WithName("GetUsersV2")
            .MapToApiVersion(2.0);

        group.MapGet("/search", Search)
            .WithName("SearchUsers");

        group.MapGet("/count", Count)
            .WithName("CountUsers");

        // Demo endpoint to trigger the global exception handler.
        group.MapGet("/boom", Boom)
            .WithName("BoomMinimal");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetUserById");

        group.MapPost("", Create)
            .WithName("CreateUser");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateUser");

        // FluentValidation via IEndpointFilter: validates UserPatchDto before entering the handler.
        group.MapPatch("/{id:int}", Patch)
            .WithName("PatchUser")
            .AddEndpointFilter<ValidationEndpointFilter<UserPatchDto>>();

        // Only the Delete endpoint requires auth, to compare directly with [Authorize] on the Controller side.
        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUser")
            .RequireAuthorization();
    }

    public static IResult GetAll(IUserService userService) =>
        TypedResults.Ok(userService.GetAll());

    // v2.0 of GetAll: changes the response shape to demonstrate API Versioning (Asp.Versioning.Http).
    // Same route "" but a different version -> mapped via .MapToApiVersion(2) in Program.cs.
    public static IResult GetAllV2(IUserService userService) =>
        TypedResults.Ok(new
        {
            apiVersion = "2.0",
            items = userService.GetAll()
        });

    // Groups query (page, pageSize) + header (X-Client-Id) into 1 object bound via [AsParameters].
    public static IResult Search([AsParameters] UserQueryParameters query, IUserService userService)
    {
        var (items, total) = userService.Search(query.Page, query.PageSize);
        return TypedResults.Ok(new
        {
            query.Page,
            query.PageSize,
            query.ClientId,
            total,
            items
        });
    }

    public static IResult GetById(int id, IUserService userService)
    {
        var user = userService.GetById(id);
        return user is not null ? TypedResults.Ok(user) : Results.NotFound();
    }

    public static IResult Create(UserCreateDto dto, IUserService userService)
    {
        var user = userService.Create(dto);
        return TypedResults.Created($"/minimal/users/{user.Id}", user);
    }

    public static IResult Update(int id, UserCreateDto dto, IUserService userService)
    {
        var user = userService.Replace(id, dto);
        return user is not null ? TypedResults.Ok(user) : Results.NotFound();
    }

    public static IResult Patch(int id, UserPatchDto dto, IUserService userService)
    {
        var user = userService.Patch(id, dto);
        return user is not null ? TypedResults.Ok(user) : Results.NotFound();
    }

    public static IResult Delete(int id, IUserService userService) =>
        userService.Delete(id) ? Results.NoContent() : Results.NotFound();

    public static IResult Count(int age, IUserService userService)
    {
        var stopwatch = Stopwatch.StartNew();
        var count = userService.CountByAge(age);
        stopwatch.Stop();

        return TypedResults.Ok(new
        {
            age,
            totalCount = count,
            elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
        });
    }

    public static IResult Boom() =>
        throw new InvalidOperationException("Demo exception from Minimal API");
}
