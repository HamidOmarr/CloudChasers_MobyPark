using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using MobyPark.DTOs.User.Request;
using MobyPark.DTOs.User.Response;
using MobyPark.Models;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Results.User;
using MobyPark.Services.Services;

namespace MobyPark.Services;

public partial class UserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<UserModel> _hasher;
    private readonly SessionService _sessions;

    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{8,}$";
    private const string PhoneTrimPattern = @"\D";
    private const string PhonePattern = @"^\+?[0-9]+$";
    private const string PhoneDigitPattern = @"^\d{9,10}$";
    private const string EmailPattern = @"^(?!\.)(?!.*\.\.)[A-Za-z0-9._%+-]+(?<!\.)@(?:(?:xn--)?[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$";

    [GeneratedRegex(PasswordPattern)]
    private static partial Regex PasswordRegex();
    [GeneratedRegex(PhoneTrimPattern)]
    private static partial Regex PhoneTrim();
    [GeneratedRegex(PhonePattern)]
    private static partial Regex Phone();
    [GeneratedRegex(PhoneDigitPattern)]
    private static partial Regex PhoneDigits();
    [GeneratedRegex(EmailPattern)]
    private static partial Regex EmailRegex();

    public UserService(IRepositoryStack repoStack, IPasswordHasher<UserModel> hasher, SessionService sessions)
    {
        _users = repoStack.Users;
        _hasher = hasher;
        _sessions = sessions;
    }

    private async Task<(bool createdSuccessfully, UserModel user)> CreateUser(UserModel user)
    {
        Validator.User(user);

        (bool createdSuccessfully, long id) = await _users.CreateWithId(user);
        if (!createdSuccessfully)
            throw new InvalidOperationException("Failed to insert user into database.");

        user.Id = id;
        return (createdSuccessfully, user);
    }

    public async Task<UserModel?> GetUserByUsername(string username)
    {
        UserModel? user = await _users.GetByUsername(username);
        return user ?? throw new KeyNotFoundException("User not found");
    }

    public async Task<UserModel?> GetUserByEmail(string email)
    {
        UserModel? user = await _users.GetByEmail(email);
        return user ?? throw new KeyNotFoundException("User not found");
    }

    public async Task<UserModel?> GetUserById(long id)
    {
        UserModel? user = await _users.GetById<UserModel>(id);
        return user ?? throw new KeyNotFoundException("User not found");
    }

    public async Task<List<UserModel>> GetAllUsers()
    {
        List<UserModel> users = await _users.GetAll();
        return users.Count == 0
            ? throw new KeyNotFoundException("No users found")
            : users;
    }

    public async Task<int> CountUsers() => await _users.Count();

    private async Task<bool> UpdateUser(UserModel user)
    {
        Validator.User(user);

        bool updatedSuccessfully = await _users.Update(user);
        return updatedSuccessfully;
    }

    public async Task<bool> UpdateUsername(long userId, string newUsername)
    {
        var user = await GetUserById(userId);
        if (user is null) throw new KeyNotFoundException("User not found");

        var existing = await GetUserByUsername(newUsername);
        if (existing is not null && existing.Id != user.Id)
            throw new ArgumentException("Username is already taken.", nameof(newUsername));

        user.Username = newUsername;

        bool updatedSuccessfully = await UpdateUser(user);
        return updatedSuccessfully;
    }

    public async Task<bool> UpdatePassword(long userId, string newPassword)
    {
        var user = await GetUserById(userId);
        if (user is null) throw new KeyNotFoundException("User not found");

        if (!PasswordRegex().IsMatch(newPassword))
            throw new ArgumentException("Password does not meet complexity requirements.", nameof(newPassword));

        user.PasswordHash = _hasher.HashPassword(user, newPassword);

        bool updatedSuccessfully = await UpdateUser(user);
        return updatedSuccessfully;
    }

    // Name should only be updated in special cases, with proper manual verification by someone with user update privileges.
    // Either first or last name can be changed, or both.
    public async Task<bool> ChangeName(long userId, string? firstName, string? lastName)
    {
        var user = await GetUserById(userId);
        if (user is null) throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrWhiteSpace(firstName))
            user.FirstName = Capitalize(firstName);
        if (!string.IsNullOrWhiteSpace(lastName))
            user.LastName = Capitalize(lastName);

        bool updatedSuccessfully = await UpdateUser(user);
        return updatedSuccessfully;
    }

    public async Task<bool> UpdateEmail(long userId, string newEmail)
    {
        var user = await GetUserById(userId);
        if (user is null) throw new KeyNotFoundException("User not found");

        string cleanEmail = CleanEmail(newEmail);
        var existingEmail = await GetUserByEmail(cleanEmail);
        if (existingEmail is not null && existingEmail.Id != user.Id)
            throw new ArgumentException("Email is already taken.", nameof(newEmail));

        user.Email = cleanEmail;

        bool updatedSuccessfully = await UpdateUser(user);
        return updatedSuccessfully;
    }

    public async Task<bool> UpdatePhone(long userId, string newPhone)
    {
        var user = await GetUserById(userId);
        if (user is null) throw new KeyNotFoundException("User not found");

        string cleanPhone = CleanPhone(newPhone);

        user.Phone = cleanPhone;

        bool updatedSuccessfully = await UpdateUser(user);
        return updatedSuccessfully;
    }

    // Birthday should only be updated in special cases, with proper manual verification by someone with user update privileges.
    public async Task<bool> UpdateBirthday(long userId, DateOnly newBirthday)
    {
        var user = await GetUserById(userId);
        if (user is null) throw new KeyNotFoundException("User not found");

        user.Birthday = newBirthday;

        bool updatedSuccessfully = await UpdateUser(user);
        return updatedSuccessfully;
    }

    public async Task<bool> DeleteUser(long id)
    {
        var user = await GetUserById(id);
        if (user is null) throw new KeyNotFoundException("User not found");

        bool createdSuccessfully = await _users.Delete(user);
        return createdSuccessfully;
    }

    public async Task<RegisterResult> CreateUserAsync(RegisterDto dto)
    {
        if (await GetUserByUsername(dto.Username) is not null)
            return new RegisterResult.UsernameTaken();

        // check name
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password) ||
            string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Phone) ||
            dto.Birthday.Year < 1900 || dto.Birthday.Year > DateTime.Now.Year)
            return new RegisterResult.InvalidData("Missing required fields");

        string cleanEmail;
        string cleanPhone;

        try
        { cleanEmail = CleanEmail(dto.Email); }
        catch (Exception e)
        { return new RegisterResult.InvalidData($"Invalid email: {e.Message}"); }
        try
        { cleanPhone = CleanPhone(dto.Phone); }
        catch (Exception e)
        { return new RegisterResult.InvalidData($"Invalid phone number: {e.Message}"); }


        if (!PasswordRegex().IsMatch(dto.Password))
            return new RegisterResult.InvalidData("Password does not meet complexity requirements.");

        var user = new UserModel
        {
            Username = dto.Username.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = cleanEmail,
            Phone = cleanPhone,
            Birthday = dto.Birthday,
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        (bool createdSuccessfully, UserModel createdUser) = await CreateUser(user);
        if (!createdSuccessfully) return new RegisterResult.Error("Failed to create user");

        return new RegisterResult.Success(createdUser);
    }

    public async Task<LoginResult> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Identifier) || string.IsNullOrWhiteSpace(dto.Password))
            return new LoginResult.Error("Identifier and password are required.");

        var user = await GetUserByEmail(dto.Identifier) ?? await GetUserByUsername(dto.Identifier);
        if (user is null)
            return new LoginResult.InvalidCredentials();

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
            return new LoginResult.InvalidCredentials();

        var token = _sessions.CreateSession(user);
        var response = new AuthDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token
        };

        return new LoginResult.Success(response);
    }

    public async Task<UpdateProfileResult> UpdateUserProfileAsync(UserModel user, UpdateProfileDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            var existing = await _users.GetByUsername(dto.Username);
            if (existing is not null && existing.Id != user.Id)
                return new UpdateProfileResult.UsernameTaken();

            user.Username = dto.Username.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var cleanEmail = CleanEmail(dto.Email);
            var existingEmail = await _users.GetByEmail(cleanEmail);
            if (existingEmail is not null && existingEmail.Id != user.Id)
                return new UpdateProfileResult.EmailTaken();

            user.Email = cleanEmail;
        }

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var cleanPhone = CleanPhone(dto.Phone);
            user.Phone = cleanPhone;
        }

        await UpdateUser(user);
        return new UpdateProfileResult.Success(user);
    }

    private static string Capitalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        input = input.Trim().ToLower(CultureInfo.InvariantCulture);
        return char.ToUpper(input[0], CultureInfo.InvariantCulture) + input[1..];
    }

    private static string CleanPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number cannot be empty.", nameof(phone));

        bool hasLeadingPlus = phone.StartsWith('+');
        phone = PhoneTrim().Replace(phone, "");
        if (hasLeadingPlus)
            phone = "+" + phone;

        if (!Phone().IsMatch(phone))
            throw new ArgumentException("Phone number contains invalid characters.", nameof(phone));

        // Normalize to +31 ...
        if (phone.StartsWith("00"))
            phone = phone[2..];
        if (phone.StartsWith('+'))
            phone = phone[1..];
        if (phone.StartsWith("31"))
            phone = phone[2..];

        if (!PhoneDigits().IsMatch(phone))
            throw new ArgumentException("Invalid Dutch phone number digits.", nameof(phone));

        if (phone.StartsWith('0'))
            phone = phone[1..];

        return "+31" + phone;
    }

    private static string CleanEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        email = email.Trim();

        var parts = email.Split('@');
        if (parts.Length != 2)
            throw new ArgumentException("Email must contain exactly one '@' symbol.", nameof(email));

        string local = parts[0];
        string domain = parts[1];

        try
        {
            var idn = new IdnMapping();
            domain = idn.GetAscii(domain);
        }
        catch (ArgumentException)
        { throw new ArgumentException("Email contains invalid international domain name.", nameof(email)); }

        string normalizedEmail = $"{local.ToLowerInvariant()}@{domain.ToLowerInvariant()}";

        if (!EmailRegex().IsMatch(normalizedEmail))
            throw new ArgumentException("Email address is not in a valid format.", nameof(email));

        return normalizedEmail;
    }
}
