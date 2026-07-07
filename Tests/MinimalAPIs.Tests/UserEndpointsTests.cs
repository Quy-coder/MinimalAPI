using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using MinimalAPIs.Endpoints;
using MinimalAPIs.Models;
using MinimalAPIs.Services;
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
}
