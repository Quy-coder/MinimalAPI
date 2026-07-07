using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalAPIs.Models;
using MinimalAPIs.Services;

namespace MinimalAPIs.Controllers;

[ApiController]
[Route("controller/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public ActionResult<List<User>> GetAll() => Ok(_userService.GetAll());

    [HttpGet("count")]
    public ActionResult<object> Count([FromQuery] int age)
    {
        var stopwatch = Stopwatch.StartNew();
        var count = _userService.CountByAge(age);
        stopwatch.Stop();

        return Ok(new
        {
            age,
            totalCount = count,
            elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
        });
    }

    // Endpoint demo để trigger global exception handler.
    [HttpGet("boom")]
    public ActionResult Boom() => throw new InvalidOperationException("Demo exception from Controller");

    [HttpGet("{id:int}")]
    public ActionResult<User> GetById(int id)
    {
        var user = _userService.GetById(id);
        return user is not null ? Ok(user) : NotFound();
    }

    [HttpPost]
    public ActionResult<User> Create(UserCreateDto dto)
    {
        var user = _userService.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public ActionResult<User> Update(int id, UserCreateDto dto)
    {
        var user = _userService.Replace(id, dto);
        return user is not null ? Ok(user) : NotFound();
    }

    [HttpPatch("{id:int}")]
    public ActionResult<User> Patch(int id, UserPatchDto dto)
    {
        var user = _userService.Patch(id, dto);
        return user is not null ? Ok(user) : NotFound();
    }

    // Chỉ endpoint Delete yêu cầu auth, để so sánh trực tiếp với .RequireAuthorization() bên Minimal API.
    [HttpDelete("{id:int}")]
    [Authorize]
    public IActionResult Delete(int id)
    {
        return _userService.Delete(id) ? NoContent() : NotFound();
    }
}
