using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public partial class UserService
{
    private readonly IDataService _dataService;
    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{8,}$";
    private const int UsernameLength = 25;
    private const int NameLength = 50;

    public UserService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public string HashPassword(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hashedPassword);
        return HashPassword(password) == hashedPassword;
    }

    public async Task<UserModel> CreateUserAsync(string username, string password, string name)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (username.Length > UsernameLength)
            throw new ArgumentException($"Username cannot exceed {UsernameLength} characters.", nameof(username));
        if (name.Length > NameLength)
            throw new ArgumentException($"Name cannot exceed {NameLength} characters.", nameof(name));

        if (!PasswordRegex().IsMatch(password))
            throw new ArgumentException("Password does not meet complexity requirements.", nameof(password));

        var hashedPassword = HashPassword(password);

        var user = new UserModel
        {
            Username = username,
            Password = hashedPassword,
            Name = name,
            Role = "USER",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dataService.Users.Create(user);
        return user;
    }


    [GeneratedRegex(PasswordPattern)]
    private static partial Regex PasswordRegex();
}
