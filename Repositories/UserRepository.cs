using System.Text.Json;
using MinimalAPIs.Models;

namespace MinimalAPIs.Repositories;

public class UserRepository : IUserRepository
{
    private readonly List<User> _users;
    private int _nextId;

    public UserRepository(IHostEnvironment env)
    {
        var filePath = Path.Combine(env.ContentRootPath, "userListMock.json");
        var json = File.ReadAllText(filePath);
        _users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
        _nextId = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
    }

    public List<User> GetAll() => _users;

    public User? GetById(int id) => _users.FirstOrDefault(u => u.Id == id);

    public User Add(User user)
    {
        var newUser = user with { Id = _nextId++ };
        _users.Add(newUser);
        return newUser;
    }

    public User? Update(int id, User user)
    {
        var index = _users.FindIndex(u => u.Id == id);
        if (index == -1) return null;

        var updated = user with { Id = id };
        _users[index] = updated;
        return updated;
    }

    public bool Delete(int id)
    {
        var index = _users.FindIndex(u => u.Id == id);
        if (index == -1) return false;

        _users.RemoveAt(index);
        return true;
    }

    public int CountByAge(int age)
    {
        var all = GetAll();
        return all.Count(u => u.Age == age);
    }
}
