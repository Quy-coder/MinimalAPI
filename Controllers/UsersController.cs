using System.Diagnostics;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalAPIs.Models;
using MinimalAPIs.Services;

namespace MinimalAPIs.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("controller/v{version:apiVersion}/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<UserPatchDto> _patchValidator;

    public UsersController(IUserService userService, IValidator<UserPatchDto> patchValidator)
    {
        _userService = userService;
        _patchValidator = patchValidator;
    }

    [HttpGet]
    [MapToApiVersion(1.0)]
    public ActionResult<List<User>> GetAll() => Ok(_userService.GetAll());

    // v2.0 của GetAll, cùng route/verb nhưng khác version -> [MapToApiVersion] để phân biệt.
    // Action khác không đánh version (Count, GetById, Create,...) mặc định phục vụ mọi version
    // khai báo trên controller ([ApiVersion(1.0)] + [ApiVersion(2.0)]).
    [HttpGet]
    [MapToApiVersion(2.0)]
    public ActionResult GetAllV2() => Ok(new { apiVersion = "2.0", items = _userService.GetAll() });

    // Không cần [AsParameters]: MVC tự đọc [FromQuery]/[FromHeader] khai báo trên từng
    // property của UserQueryParameters khi tham số action được đánh [FromQuery].
    [HttpGet("search")]
    public ActionResult Search([FromQuery] UserQueryParameters query)
    {
        var (items, total) = _userService.Search(query.Page, query.PageSize);
        return Ok(new { query.Page, query.PageSize, query.ClientId, total, items });
    }

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

    // Controller cách validate bằng FluentValidation: inject IValidator<T> qua DI, gọi thủ công
    // trong action (khác với UsersController.GetAll/Create dùng [ApiController] + DataAnnotations
    // tự động qua ModelState). So sánh với Minimal API dùng ValidationEndpointFilter<T>.
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<User>> Patch(int id, UserPatchDto dto)
    {
        var validationResult = await _patchValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

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
