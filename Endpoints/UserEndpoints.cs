using System.Diagnostics;
using Asp.Versioning;
using MinimalAPIs.Filters;
using MinimalAPIs.Models;
using MinimalAPIs.Services;

namespace MinimalAPIs.Endpoints;

// Handler tách ra static method (thay vì lambda inline trong Program.cs) để unit-test được
// như một hàm bình thường, không cần host app hay dựng ControllerContext.
//
// Route mapping cũng gom về đây thành 1 extension method (MapUserEndpoints), thay vì để
// từng .MapGet/.MapPost rải trong Program.cs. Khi thêm feature mới (Order, Product,...) chỉ
// cần thêm 1 class OrderEndpoints tương tự + 1 dòng app.MapOrderEndpoints() trong Program.cs,
// Program.cs không phình to theo số lượng endpoint.
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Version set riêng cho nhóm Users: khai báo version nào tồn tại, endpoint không gọi
        // .MapToApiVersion(...) mặc định phục vụ mọi version trong set (giống Controller).
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

        // v2.0 của GetAll, cùng route/verb với bản v1 -> bắt buộc .MapToApiVersion để phân biệt.
        group.MapGet("", GetAllV2)
            .WithName("GetUsersV2")
            .MapToApiVersion(2.0);

        group.MapGet("/search", Search)
            .WithName("SearchUsers");

        group.MapGet("/count", Count)
            .WithName("CountUsers");

        // Endpoint demo để trigger global exception handler.
        group.MapGet("/boom", Boom)
            .WithName("BoomMinimal");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetUserById");

        group.MapPost("", Create)
            .WithName("CreateUser");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateUser");

        // FluentValidation qua IEndpointFilter: validate UserPatchDto trước khi vào handler.
        group.MapPatch("/{id:int}", Patch)
            .WithName("PatchUser")
            .AddEndpointFilter<ValidationEndpointFilter<UserPatchDto>>();

        // Chỉ endpoint Delete yêu cầu auth, để so sánh trực tiếp với [Authorize] bên Controller.
        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUser")
            .RequireAuthorization();
    }

    public static IResult GetAll(IUserService userService) =>
        TypedResults.Ok(userService.GetAll());

    // v2.0 của GetAll: đổi shape response để minh hoạ API Versioning (Asp.Versioning.Http).
    // Cùng route "" nhưng khác version -> map bằng .MapToApiVersion(2) trong Program.cs.
    public static IResult GetAllV2(IUserService userService) =>
        TypedResults.Ok(new
        {
            apiVersion = "2.0",
            items = userService.GetAll()
        });

    // Gom query (page, pageSize) + header (X-Client-Id) vào 1 object bind bằng [AsParameters].
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
