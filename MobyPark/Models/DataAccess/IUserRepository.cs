using MobyPark.Models;

namespace MobyPark.Models.DataService;

public interface IUserRepository
{
    Task<UserModel?> GetByEmailOrUsernameAsync(string identifier);
    Task<bool> ExistsByEmailOrUsernameAsync(string email, string username);
    Task AddAsync(UserModel user);
    
    Task<UserModel?> GetByUsernameAsync(string username); // NEW
    Task<UserModel?> GetByEmailAsync(string email);       // NEW
    
    Task UpdateAsync(UserModel user); 
}