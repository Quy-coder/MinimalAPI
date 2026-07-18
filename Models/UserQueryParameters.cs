using Microsoft.AspNetCore.Mvc;

namespace MinimalAPIs.Models;

// Groups multiple parameter sources (query string + header) into 1 object, instead of declaring
// each parameter separately on the handler/action signature.
// - Minimal API: bind via [AsParameters] directly on the handler parameter (see UserEndpoints.Search).
// - Controller: no attribute wrapper needed, just [FromQuery] on the action parameter;
//   MVC reads the [FromQuery]/[FromHeader] declared on each property inside it automatically (see UsersController.Search).
// [FromQuery]/[FromHeader] here are shared between both styles because they're the same
// Microsoft.AspNetCore.Mvc attributes, recognized by both Minimal API and MVC via the binding-source interface.
public class UserQueryParameters
{
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 10;

    [FromHeader(Name = "X-Client-Id")]
    public string? ClientId { get; init; }
}
