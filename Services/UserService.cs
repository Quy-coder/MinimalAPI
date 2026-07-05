using MinimalAPIs.Models;
using MinimalAPIs.Repositories;

namespace MinimalAPIs.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public List<User> GetAll() => _repository.GetAll();

    public User? GetById(int id) => _repository.GetById(id);

    public User Create(UserCreateDto dto) =>
        _repository.Add(new User(0, dto.Name, dto.Email, dto.Age));

    public User? Replace(int id, UserCreateDto dto) =>
        _repository.Update(id, new User(id, dto.Name, dto.Email, dto.Age));

    public User? Patch(int id, UserPatchDto dto)
    {
        var current = _repository.GetById(id);
        if (current is null) return null;

        var updated = current with
        {
            Name = dto.Name ?? current.Name,
            Email = dto.Email ?? current.Email,
            Age = dto.Age ?? current.Age
        };
        return _repository.Update(id, updated);
    }

    public bool Delete(int id) => _repository.Delete(id);

    public int CountByAge(int age) => _repository.CountByAge(age);
}
