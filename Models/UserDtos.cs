namespace MinimalAPIs.Models;

public record UserCreateDto(string Name, string Email, int Age);
public record UserPatchDto(string? Name, string? Email, int? Age);
