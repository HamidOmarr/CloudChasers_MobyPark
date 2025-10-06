using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public partial class UserService
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher<UserModel> _hasher;
    private readonly SessionService _sessions;
    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{8,}$";
    private const int UsernameLength = 25;
    private const int NameLength = 50;
    
    public UserService(IUserRepository repo,
        IPasswordHasher<UserModel> hasher,
        SessionService sessions)
    {
        _repo = repo;
        _hasher = hasher;
        _sessions = sessions;
    }

    public async Task<UserModel> CreateUserAsync(string username, string password, string firstName, string lastName, string email, string phone, DateTime birthday)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        if (username.Length > UsernameLength)
            throw new ArgumentException($"Username cannot exceed {UsernameLength} characters.", nameof(username));
        if (firstName.Length > NameLength)
            throw new ArgumentException($"First name cannot exceed {NameLength} characters.", nameof(firstName));
        if (lastName.Length > NameLength)
            throw new ArgumentException($"Last mame cannot exceed {NameLength} characters.", nameof(lastName));

        if (!PasswordRegex().IsMatch(password))
            throw new ArgumentException("Password does not meet complexity requirements.", nameof(password));
        //var existingByUser = await _dataAccess.Users.GetByUsername(username);
       // var existingByEmail = await _dataAccess.Users.GetByEmail(email);
        //if (existingByUser is not null || existingByEmail is not null)
          //  throw new InvalidOperationException("Email address or username already in use");
          if (await _repo.ExistsByEmailOrUsernameAsync(email, username))
              throw new InvalidOperationException("Email address or username already in use");
          
        var user = new UserModel
        {
            Username = username.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim(),
            Phone = phone.Trim(),
            BirthYear = birthday.Year,
            Role = "USER",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, password); 
        await _repo.AddAsync(user);
        //await _dataAccess.Users.Create(user);
        return user;
    }

    public async Task<AuthResponse> LoginAsync(string identifier, string password)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Identifier and password are required.");

        //var user = await _dataAccess.Users.GetByEmail(identifier) ?? await _dataAccess.Users.GetByUsername(identifier);
        var user = await _repo.GetByEmailOrUsernameAsync(identifier.Trim());
        if (user is null)
            throw new InvalidOperationException("Invalid credentials");

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid credentials");

        var token = _sessions.CreateSession(user);
        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token
        };
    } 

    /*public async Task<UserModel?> GetUserByUsername(string username)
    {
        UserModel? user = await _dataAccess.Users.GetByUsername(username);
        return user;
    }*/
    
    public async Task<UserModel?> GetUserByUsername(string username)
    {
        var user = await _repo.GetByUsernameAsync(username);
        return user;
    }

    public async Task<UserModel> UpdateUser(UserModel user)
    {
        ArgumentNullException.ThrowIfNull(user);
        await _repo.UpdateAsync(user);
        //await _dataAccess.Users.Update(user);
        return user;
    }

    [GeneratedRegex(PasswordPattern)]
    private static partial Regex PasswordRegex();
}
