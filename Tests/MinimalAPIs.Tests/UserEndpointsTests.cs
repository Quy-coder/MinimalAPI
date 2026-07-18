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

// Handler Minimal API là static method (tách ra từ Program.cs) -> test bằng cách gọi thẳng
// hàm với tham số giả, không cần host app, không cần dựng ControllerContext/HttpContext.
// Case y hệt UsersControllerTests để so sánh trực tiếp 2 style.
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

    // [AsParameters] bind UserQueryParameters (page/pageSize từ query, ClientId từ header) -> gọi thẳng
    // handler với object đã dựng sẵn, không cần host app hay giả lập HTTP request.
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

    // API Versioning: handler v2.0 tồn tại song song với GetAll (v1.0), khác shape response.
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

// ValidationEndpointFilter<T> là 1 class thường (IEndpointFilter) -> test được độc lập bằng cách
// tự dựng EndpointFilterInvocationContext, không cần host app hay TestServer.
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
