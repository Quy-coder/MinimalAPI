using System.Diagnostics;
using MinimalAPIs.Models;
using MinimalAPIs.Services;

namespace MinimalAPIs.Endpoints;

// Handler tách ra static method (thay vì lambda inline trong Program.cs) để unit-test được
// như một hàm bình thường, không cần host app hay dựng ControllerContext.
public static class UserEndpoints
{
    public static IResult GetAll(IUserService userService) =>
        TypedResults.Ok(userService.GetAll());

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
