using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using MobyPark.Models;
using MobyPark.Models.Responses.User;
using MobyPark.Models.DataService;
using MobyPark.Models.Requests.User;
using MobyPark.Services.Results.User;

namespace MobyPark.Services;

public partial class UserService
{
    private readonly IDataAccess _dataAccess;
    private readonly IPasswordHasher<UserModel> _hasher;
    private readonly SessionService _sessions;

    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{8,}$";
    private const string PhoneTrimPattern = @"\D";
    private const string PhonePattern = @"^\+?[0-9]+$";
    private const string PhoneDigitPattern = @"^0\d{9}$";
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

    public UserService(IDataAccess dataAccess, IPasswordHasher<UserModel> hasher, SessionService sessions)
    {
        _dataAccess = dataAccess;
        _hasher = hasher;
        _sessions = sessions;
    }

    // Generic CRUD operations
    private async Task<(bool success, UserModel user)> CreateUser(UserModel user)
    {
        (bool success, int id) = await _dataAccess.Users.CreateWithId(user);
        if (!success)
            throw new InvalidOperationException("Failed to insert user into database.");

        user.Id = id;
        return (success, user);
    }

    public async Task<UserModel?> GetUserByUsername(string username) =>  await _dataAccess.Users.GetByUsername(username);

    public async Task<UserModel?> GetUserByEmail(string email) => await _dataAccess.Users.GetByEmail(email);

    public async Task<UserModel?> GetUserById(int id) => await _dataAccess.Users.GetById(id);

    public async Task<List<UserModel>> GetAllUsers() => await _dataAccess.Users.GetAll();

    public async Task<int> CountUsers() => await _dataAccess.Users.Count();

    public async Task<UserModel> UpdateUser(UserModel user)
    {
        ArgumentNullException.ThrowIfNull(user);
        await _dataAccess.Users.Update(user);
        return user;
    }

    public async Task<bool> DeleteUser(int id)
    {
        var user = await GetUserById(id);
        if (user is null) throw new KeyNotFoundException("User not found");

        bool success = await _dataAccess.Users.Delete(id);
        return success;
    }

    // Direct methods
    public async Task<RegisterResult> CreateUserAsync(RegisterRequest request)
    {
        if (await GetUserByUsername(request.Username) is not null)
            return new RegisterResult.UsernameTaken();

        // check name
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Phone) || request.Birthday.Year < 1900 ||
            request.Birthday.Year > DateTime.Now.Year)
            return new RegisterResult.InvalidData("Missing required fields");

        string cleanEmail;
        string cleanPhone;

        try
        { cleanEmail = CleanEmail(request.Email); }
        catch (Exception e)
        { return new RegisterResult.InvalidData($"Invalid email: {e.Message}"); }
        try
        { cleanPhone = CleanPhone(request.Phone); }
        catch (Exception e)
        { return new RegisterResult.InvalidData($"Invalid phone number: {e.Message}"); }


        if (!PasswordRegex().IsMatch(request.Password))
            return new RegisterResult.InvalidData("Password does not meet complexity requirements.");

        var user = new UserModel
        {
            Username = request.Username.Trim(),
            Name = request.Name.Trim(),
            Email = cleanEmail,
            Phone = cleanPhone,
            BirthYear = request.Birthday.Year,
            Role = "USER",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        (bool success, UserModel createdUser) = await CreateUser(user);
        if (!success) return new RegisterResult.Error("Failed to create user");

        return new RegisterResult.Success(createdUser);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
            return new LoginResult.Error("Identifier and password are required.");

        var user = await GetUserByEmail(request.Identifier) ?? await GetUserByUsername(request.Identifier);
        if (user is null)
            return new LoginResult.InvalidCredentials();

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return new LoginResult.InvalidCredentials();

        var token = _sessions.CreateSession(user);
        var response = new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token
        };

        return new LoginResult.Success(response);
    }

    public async Task<UpdateProfileResult> UpdateUserProfileAsync(UserModel user, UpdateProfileRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var existing = await _dataAccess.Users.GetByUsername(request.Username);
            if (existing is not null && existing.Id != user.Id)
                return new UpdateProfileResult.UsernameTaken();

            user.Username = request.Username.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var cleanEmail = CleanEmail(request.Email);
            var existingEmail = await _dataAccess.Users.GetByEmail(cleanEmail);
            if (existingEmail is not null && existingEmail.Id != user.Id)
                return new UpdateProfileResult.EmailTaken();

            user.Email = cleanEmail;
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var cleanPhone = CleanPhone(request.Phone);
            user.Phone = cleanPhone;
        }

        await UpdateUser(user);
        return new UpdateProfileResult.Success(user);
    }


    // Helpers for cleaning and validation
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

        // Normalize to +310 ...
        if (phone.StartsWith("00"))
            phone = phone[2..];
        if (phone.StartsWith('+'))
            phone = phone[1..];
        if (phone.StartsWith("31"))
            phone = phone[2..];
        if (!phone.StartsWith('0'))
            phone = "0" + phone;

        if (!PhoneDigits().IsMatch(phone))
            throw new ArgumentException("Invalid Dutch phone number digits.", nameof(phone));

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
