using Microsoft.AspNetCore.Mvc;

namespace MinimalAPIs.Models;

// Gom nhiều nguồn tham số (query string + header) vào 1 object, thay vì khai báo từng
// tham số rời rạc trên chữ ký handler/action.
// - Minimal API: bind bằng [AsParameters] ngay tại tham số handler (xem UserEndpoints.Search).
// - Controller: không cần attribute wrapper, chỉ cần [FromQuery] trên tham số action;
//   MVC tự đọc [FromQuery]/[FromHeader] khai báo trên từng property bên trong (xem UsersController.Search).
// [FromQuery]/[FromHeader] ở đây dùng chung được cho cả 2 style vì cùng là attribute của
// Microsoft.AspNetCore.Mvc, được cả Minimal API lẫn MVC nhận diện qua interface binding-source.
public class UserQueryParameters
{
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 10;

    [FromHeader(Name = "X-Client-Id")]
    public string? ClientId { get; init; }
}
