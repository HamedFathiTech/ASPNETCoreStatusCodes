using ASPNETCoreStatusCodes.Models;

namespace ASPNETCoreStatusCodes.Services;
// ReSharper disable All
public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(int id, User user);
    Task<User?> PatchUserAsync(int id, UserPatchDto patchDto);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> UserExistsAsync(int id);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
}