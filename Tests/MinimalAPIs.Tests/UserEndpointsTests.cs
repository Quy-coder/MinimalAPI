using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MinimalAPIs.Endpoints;
using MinimalAPIs.Filters;
using MinimalAPIs.Models;
using MinimalAPIs.Services;
using MinimalAPIs.Validators;
using Xunit;

namespace MinimalAPIs.Tests;

// Minimal API handlers are static methods (extracted from Program.cs) -> tested by calling the
// function directly with fake parameters, no host app, no ControllerContext/HttpContext to build.
// Cases mirror UsersControllerTests exactly to compare the 2 styles directly.
public class UserEndpointsTests
{
    [Fact]
    public void GetById_ReturnsOk_WhenUserExists()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.GetById(1)).Returns(new User(1, "A", "a@test.com", 20));

        var result = UserEndpoints.GetById(1, mockService.Object);

        var okResult = Assert.IsType<Ok<User>>(result);
        Assert.Equal(1, okResult.Value!.Id);
    }

    [Fact]
    public void GetById_ReturnsNotFound_WhenUserMissing()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.GetById(99)).Returns((User?)null);

        var result = UserEndpoints.GetById(99, mockService.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public void Create_ReturnsCreated()
    {
        var dto = new UserCreateDto("New", "new@test.com", 25);
        var created = new User(10, dto.Name, dto.Email, dto.Age);
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Create(dto)).Returns(created);

        var result = UserEndpoints.Create(dto, mockService.Object);

        var createdResult = Assert.IsType<Created<User>>(result);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public void Delete_ReturnsNoContent_WhenDeleted()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Delete(1)).Returns(true);

        var result = UserEndpoints.Delete(1, mockService.Object);

        Assert.IsType<NoContent>(result);
    }

    [Fact]
    public void Delete_ReturnsNotFound_WhenMissing()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Delete(99)).Returns(false);

        var result = UserEndpoints.Delete(99, mockService.Object);

        Assert.IsType<NotFound>(result);
    }

    // [AsParameters] binds UserQueryParameters (page/pageSize from query, ClientId from header) -> call the
    // handler directly with a pre-built object, no host app or simulated HTTP request needed.
    [Fact]
    public void Search_ReturnsOk_WithPagedItems()
    {
        var users = new List<User> { new(1, "A", "a@test.com", 20) };
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Search(1, 10)).Returns((users, 1));

        var result = UserEndpoints.Search(new UserQueryParameters { Page = 1, PageSize = 10 }, mockService.Object);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ((IStatusCodeHttpResult)result).StatusCode);
    }

    // API Versioning: the v2.0 handler exists alongside GetAll (v1.0), with a different response shape.
    [Fact]
    public void GetAllV2_ReturnsOk()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.GetAll()).Returns([]);

        var result = UserEndpoints.GetAllV2(mockService.Object);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ((IStatusCodeHttpResult)result).StatusCode);
    }
}

// ValidationEndpointFilter<T> is a plain class (IEndpointFilter) -> can be tested independently by
// building an EndpointFilterInvocationContext directly, no host app or TestServer needed.
public class ValidationEndpointFilterTests
{
    private static EndpointFilterInvocationContext CreateContext(UserPatchDto dto)
    {
        var services = new ServiceCollection();
        services.AddScoped<FluentValidation.IValidator<UserPatchDto>, UserPatchDtoValidator>();

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        return EndpointFilterInvocationContext.Create(httpContext, dto);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsValidationProblem_WhenDtoInvalid()
    {
        var filter = new ValidationEndpointFilter<UserPatchDto>();
        var context = CreateContext(new UserPatchDto(null, null, null));

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.IsAssignableFrom<IResult>(result);
        Assert.IsNotType<Ok>(result);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenDtoValid()
    {
        var filter = new ValidationEndpointFilter<UserPatchDto>();
        var context = CreateContext(new UserPatchDto("Valid Name", null, null));

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.IsType<Ok>(result);
    }
}
