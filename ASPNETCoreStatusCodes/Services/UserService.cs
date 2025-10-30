using ASPNETCoreStatusCodes.Models;

namespace ASPNETCoreStatusCodes.Services;
// ReSharper disable All

public class UserService : IUserService
{
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public UserService()
    {
        // Seed with sample data
        _users.AddRange([
            new User
            {
                Id = _nextId++,
                Email = "john.doe@example.com",
                FirstName = "John",
                LastName = "Doe",
                Age = 30,
                IsSystemAccount = false
            },
            new User
            {
                Id = _nextId++,
                Email = "jane.smith@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                Age = 25,
                IsSystemAccount = true
            }
        ]);
    }

    public Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return Task.FromResult(_users.Where(u => u.IsActive).AsEnumerable());
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id && u.IsActive));
    }

    public Task<User?> GetUserByEmailAsync(string email)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Email == email && u.IsActive));
    }

    public Task<User> CreateUserAsync(User user)
    {
        user.Id = _nextId++;
        user.CreatedAt = DateTime.UtcNow;
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<User?> UpdateUserAsync(int id, User user)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == id);
        if (existingUser == null) return Task.FromResult<User?>(null);

        existingUser.Email = user.Email;
        existingUser.FirstName = user.FirstName;
        existingUser.LastName = user.LastName;
        existingUser.Age = user.Age;
        existingUser.IsActive = user.IsActive;
        existingUser.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<User?>(existingUser);
    }

    public Task<User?> PatchUserAsync(int id, UserPatchDto patchDto)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == id);
        if (existingUser == null) return Task.FromResult<User?>(null);

        if (!string.IsNullOrEmpty(patchDto.Email))
            existingUser.Email = patchDto.Email;
        if (!string.IsNullOrEmpty(patchDto.FirstName))
            existingUser.FirstName = patchDto.FirstName;
        if (!string.IsNullOrEmpty(patchDto.LastName))
            existingUser.LastName = patchDto.LastName;
        if (patchDto.Age.HasValue)
            existingUser.Age = patchDto.Age.Value;
        if (patchDto.IsActive.HasValue)
            existingUser.IsActive = patchDto.IsActive.Value;

        existingUser.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult<User?>(existingUser);
    }

    public Task<bool> DeleteUserAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null) return Task.FromResult(false);

        // Soft delete
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> UserExistsAsync(int id)
    {
        return Task.FromResult(_users.Any(u => u.Id == id && u.IsActive));
    }

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        return Task.FromResult(_users.Any(u => u.Email == email && u.IsActive && (excludeId == null || u.Id != excludeId)));
    }
}