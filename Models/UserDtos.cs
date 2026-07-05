using System.ComponentModel.DataAnnotations;

namespace MinimalAPIs.Models;

public record UserCreateDto(
    [Required]
    string Name,
    string Email,
    int Age
);

public record UserPatchDto(string? Name, string? Email, int? Age);
