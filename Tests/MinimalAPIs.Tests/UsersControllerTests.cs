using Microsoft.AspNetCore.Mvc;
using Moq;
using MinimalAPIs.Controllers;
using MinimalAPIs.Models;
using MinimalAPIs.Services;
using MinimalAPIs.Validators;
using Xunit;

namespace MinimalAPIs.Tests;

// Controller là class -> test bằng cách new trực tiếp + mock constructor dependency.
// Không cần host app, không cần dựng ControllerContext cho các case dưới đây.
// UserPatchDtoValidator không có dependency nên dùng instance thật thay vì mock.
public class UsersControllerTests
{
    private static UsersController CreateController(Mock<IUserService> mockService) =>
        new(mockService.Object, new UserPatchDtoValidator());

    [Fact]
    public void GetById_ReturnsOk_WhenUserExists()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.GetById(1)).Returns(new User(1, "A", "a@test.com", 20));
        var controller = CreateController(mockService);

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
        var controller = CreateController(mockService);

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
        var controller = CreateController(mockService);

        var result = controller.Create(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public void Delete_ReturnsNoContent_WhenDeleted()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Delete(1)).Returns(true);
        var controller = CreateController(mockService);

        var result = controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Delete_ReturnsNotFound_WhenMissing()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Delete(99)).Returns(false);
        var controller = CreateController(mockService);

        var result = controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // FluentValidation qua DI, gọi thủ công trong action: dto rỗng -> ValidationProblem, service không được gọi.
    [Fact]
    public async Task Patch_ReturnsValidationProblem_WhenAllFieldsNull()
    {
        var mockService = new Mock<IUserService>();
        var controller = CreateController(mockService);

        var result = await controller.Patch(1, new UserPatchDto(null, null, null));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        mockService.Verify(s => s.Patch(It.IsAny<int>(), It.IsAny<UserPatchDto>()), Times.Never);
    }

    [Fact]
    public async Task Patch_ReturnsOk_WhenValid()
    {
        var dto = new UserPatchDto("New Name", null, null);
        var updated = new User(1, "New Name", "a@test.com", 20);
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Patch(1, dto)).Returns(updated);
        var controller = CreateController(mockService);

        var result = await controller.Patch(1, dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(updated, okResult.Value);
    }

    // [AsParameters] bên Minimal API tương ứng với [FromQuery] trên UserQueryParameters bên Controller.
    [Fact]
    public void Search_ReturnsOk_WithPagedItems()
    {
        var users = new List<User> { new(1, "A", "a@test.com", 20) };
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.Search(1, 10)).Returns((users, 1));
        var controller = CreateController(mockService);

        var result = controller.Search(new UserQueryParameters { Page = 1, PageSize = 10 });

        Assert.IsType<OkObjectResult>(result);
    }

    // API Versioning: action v2.0 trả về shape khác so với GetAll (v1.0).
    [Fact]
    public void GetAllV2_ReturnsOk()
    {
        var mockService = new Mock<IUserService>();
        mockService.Setup(s => s.GetAll()).Returns([]);
        var controller = CreateController(mockService);

        var result = controller.GetAllV2();

        Assert.IsType<OkObjectResult>(result);
    }
}
