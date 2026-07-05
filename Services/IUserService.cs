using MinimalAPIs.Models;

namespace MinimalAPIs.Services;

public interface IUserService
{
    List<User> GetAll();
    User? GetById(int id);
    User Create(UserCreateDto dto);
    User? Replace(int id, UserCreateDto dto);
    User? Patch(int id, UserPatchDto dto);
    bool Delete(int id);
    int CountByAge(int age);
}
