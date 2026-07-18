using MinimalAPIs.Models;

namespace MinimalAPIs.Repositories;

public interface IUserRepository
{
    List<User> GetAll();
    User? GetById(int id);
    User Add(User user);
    User? Update(int id, User user);
    bool Delete(int id);
    int CountByAge(int age);
    (List<User> Items, int Total) Search(int page, int pageSize);
}
