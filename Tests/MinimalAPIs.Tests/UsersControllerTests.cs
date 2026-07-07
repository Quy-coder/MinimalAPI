using Microsoft.AspNetCore.Mvc;
using Moq;
using MinimalAPIs.Controllers;
using MinimalAPIs.Models;
using MinimalAPIs.Services;
using Xunit;

namespace MinimalAPIs.Tests;

// Controller là class -> test bằng cách new trực tiếp + mock constructor dependency.
// Không cần host app, không cần dựng ControllerContext cho các case dưới đây.
public class UsersControllerTests
{
    [Fact]
    public void GetById_ReturnsOk_WhenUserExists()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.GetById(1)).Returns(new User(1, "A", "a@test.com", 20));
        var controller = new UsersController(mockService.Object);

        var result = controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<User>(okResult.Value);
        Assert.Equal(1, user.Id);
    }

    [Fact]
    public void GetById_ReturnsNotFound_WhenUserMissing()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.GetById(99)).Returns((User?)null);
        var controller = new UsersController(mockService.Object);

        var result = controller.GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void Create_ReturnsCreatedAtAction()
    {
        var dto = new UserCreateDto("New", "new@test.com", 25);
        var created = new User(10, dto.Name, dto.Email, dto.Age);
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Create(dto)).Returns(created);
        var controller = new UsersController(mockService.Object);

        var result = controller.Create(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public void Delete_ReturnsNoContent_WhenDeleted()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Delete(1)).Returns(true);
        var controller = new UsersController(mockService.Object);

        var result = controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Delete_ReturnsNotFound_WhenMissing()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Delete(99)).Returns(false);
        var controller = new UsersController(mockService.Object);

        var result = controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
