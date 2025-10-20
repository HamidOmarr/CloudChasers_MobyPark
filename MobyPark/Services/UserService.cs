using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using MobyPark.DTOs.User.Request;
using MobyPark.DTOs.User.Response;
using MobyPark.Models;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.User;
using MobyPark.Validation;

namespace MobyPark.Services;

public partial class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUserPlateRepository _userPlates;
    private readonly IParkingSessionRepository _parkingSessions;
    private readonly IRoleRepository _roles;
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

    public UserService(
        IUserRepository users,
        IUserPlateRepository userPlates,
        IParkingSessionRepository parkingSessions,
        IRoleRepository roles,
        IPasswordHasher<UserModel> hasher,
        SessionService sessions)
    {
        _users = users;
        _userPlates = userPlates;
        _parkingSessions = parkingSessions;
        _roles = roles;
        _hasher = hasher;
        _sessions = sessions;
    }

    private async Task<UserModel> CreateUser(UserModel user)
    {
        (bool createdSuccessfully, long id) = await _users.CreateWithId(user);

        if (!createdSuccessfully) throw new InvalidOperationException("Database insertion failed unexpectedly.");

        user.Id = id;
        return user;
    }

    public async Task<UserModel?> GetUserByUsername(string username) => await _users.GetByUsername(username);

    public async Task<UserModel?> GetUserByEmail(string email) => await _users.GetByEmail(email);

    public async Task<UserModel?> GetUserById(long id) => await _users.GetById<UserModel>(id);

    public async Task<List<UserModel>> GetAllUsers() => await _users.GetAll();

    public async Task<int> CountUsers() => await _users.Count();

    private async Task<bool> UpdateUser(UserModel user) => await _users.Update(user);

    public async Task<bool> DeleteUser(long id)
    {
        var user = await GetUserById(id);
        if (user is null) return false;  // User does not exist. Currently returning false, as there isn't a NotFound result type here yet.

        bool createdSuccessfully = await _users.Delete(user);
        return createdSuccessfully;
    }

    public async Task<RegisterResult> CreateUserAsync(RegisterDto dto)
    {
        if (await GetUserByUsername(dto.Username) is not null)
            return new RegisterResult.UsernameTaken();

        var emailResult = CleanEmail(dto.Email, out var cleanEmail);
        if (emailResult is RegisterResult.InvalidData invalidEmail)
            return invalidEmail;
        var phoneResult = CleanPhone(dto.Phone, out var cleanPhone);
        if (phoneResult is RegisterResult.InvalidData invalidPhone)
            return invalidPhone;

        if (!PasswordRegex().IsMatch(dto.Password))
            return new RegisterResult.InvalidData("Password does not meet complexity requirements.");

        var user = new UserModel
        {
            Username = dto.Username.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = cleanEmail!,
            Phone = cleanPhone!,
            Birthday = dto.Birthday,
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        try
        {
            UserModel createdUser = await CreateUser(user);
            if (string.IsNullOrWhiteSpace(dto.LicensePlate)) return new RegisterResult.Success(createdUser);

            var plateResult = await ValidateGuestLicensePlate(createdUser.Id, dto.LicensePlate);
            return plateResult ?? new RegisterResult.Success(createdUser);
        }
        catch (InvalidOperationException)
        { return new RegisterResult.Error("Failed to create user due to database issue."); }
    }

    private async Task<RegisterResult?> ValidateGuestLicensePlate(long userId, string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate)) return null;

        licensePlate = ValHelper.NormalizePlate(licensePlate);

        var userPlates = await _userPlates.GetPlatesByPlate(licensePlate);
        foreach (var uPlate in userPlates)
        {
            var user = await _users.GetById<UserModel>(uPlate.UserId);
            if (user is null || user.Id == UserRepository.DeletedUserId) continue;

            var userRecentSessions =
                await _parkingSessions.GetAllRecentSessionsByLicensePlate(uPlate.LicensePlateNumber, TimeSpan.FromDays(30));
            if (userRecentSessions.Count > 0)
                return new RegisterResult.InvalidData("License plate is already associated with another user.");
        }

        await _userPlates.AddPlateToUser(userId, licensePlate);
        return null;
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
        // Username
        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            var existing = await _users.GetByUsername(dto.Username);
            if (existing is not null && existing.Id != user.Id)
                return new UpdateProfileResult.UsernameTaken();

            user.Username = dto.Username.Trim();
        }

        // Password
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var passwordResult = ValidatePasswordIntegrity(dto.Password);
            if (passwordResult is UpdateProfileResult.InvalidData)
                return passwordResult;

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        }

        // Email
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var emailResult = ValidateAndCleanEmail(dto.Email, out string? cleanEmail);
            if (emailResult is not UpdateProfileResult.Success)
                return emailResult;

            var existingEmail = await _users.GetByEmail(cleanEmail!);
            if (existingEmail is not null && existingEmail.Id != user.Id)
                return new UpdateProfileResult.EmailTaken();

            user.Email = cleanEmail!;
        }

        // Phone
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var phoneResult = ValidateAndCleanPhone(dto.Phone, out string? cleanPhone);
            if (phoneResult is UpdateProfileResult.InvalidData)
                return phoneResult;

            user.Phone = cleanPhone!;
        }

        bool updatedSuccessfully = await UpdateUser(user);

        if (!updatedSuccessfully)
            return new UpdateProfileResult.Error("Failed to update user profile.");
        return new UpdateProfileResult.Success(user);
    }

    public async Task<UpdateProfileResult> UpdateUserIdentityAsync(long userId, string? newFirstName,
        string? newLastName, DateOnly? newBirthday)
    {
        // Should only be callable by someone with USERS:MANAGE permission
        var user = await GetUserById(userId);
        if (user is null) return new UpdateProfileResult.NotFound();

        bool wasModified = false;

        if (!string.IsNullOrWhiteSpace(newFirstName))
        {
            user.FirstName = Capitalize(newFirstName);
            wasModified = true;
        }
        if (!string.IsNullOrWhiteSpace(newLastName))
        {
            user.LastName = Capitalize(newLastName);
            wasModified = true;
        }

        if (newBirthday.HasValue)
        {
            if (newBirthday.Value > DateOnly.FromDateTime(DateTime.Now))
                return new UpdateProfileResult.InvalidData("Birthday cannot be in the future.");

            if (newBirthday.Value.Year < 1900)
                return new UpdateProfileResult.InvalidData("Birthday is not valid.");

            var today = DateOnly.FromDateTime(DateTime.Now);
            int age = today.Year - newBirthday.Value.Year;
            if (newBirthday.Value > today.AddYears(-age)) age--;
            // Minimum age requirement; provisional license considered to be valid from 16 years old
            if (age < 16) return new UpdateProfileResult.InvalidData("User must be at least 16 years old.");

            user.Birthday = newBirthday.Value;
            wasModified = true;
        }

        if (!wasModified) return new UpdateProfileResult.Success(user);

        bool updatedSuccessfully = await UpdateUser(user);

        if (!updatedSuccessfully) return new UpdateProfileResult.Error("Failed to save changes to the database (Admin Identity Update).");

        return new UpdateProfileResult.Success(user);
    }

    public async Task<UpdateProfileResult> UpdateUserRoleAsync(long userId, long roleId)
    {
        // Should only be callable by someone with USERS:MANAGE permission
        var user = await GetUserById(userId);
        if (user is null) return new UpdateProfileResult.NotFound();

        var newRole = await _roles.GetById<RoleModel>(roleId);
        if (newRole is null) return new UpdateProfileResult.InvalidData("Role does not exist.");

        if (user.Role.Name == "ADMIN" && newRole.Name != "ADMIN")
            return new UpdateProfileResult.InvalidData("Cannot change role of an Admin user.");

        user.RoleId = roleId;

        bool updated = await UpdateUser(user);
        if (!updated)
            return new UpdateProfileResult.Error("Failed to update user role.");

        return new UpdateProfileResult.Success(user);
    }

    private static UpdateProfileResult ValidatePasswordIntegrity(string password)
    {
        if (!PasswordRegex().IsMatch(password))
            return new UpdateProfileResult.InvalidData("Password does not meet complexity requirements.");
        return new UpdateProfileResult.Success(null!);
    }

    private static UpdateProfileResult ValidateAndCleanEmail(string email, out string? cleanEmail)
    {
        var registerResult = CleanEmail(email, out cleanEmail);

        if (registerResult is RegisterResult.InvalidData invalidData)
            return new UpdateProfileResult.InvalidData($"Email format error: {invalidData.Message}");
        return new UpdateProfileResult.Success(null!);
    }

    private static UpdateProfileResult ValidateAndCleanPhone(string phone, out string? cleanPhone)
    {
        var registerResult = CleanPhone(phone, out cleanPhone);

        if (registerResult is RegisterResult.InvalidData invalidData)
            return new UpdateProfileResult.InvalidData($"Phone format error: {invalidData.Message}");
        return new UpdateProfileResult.Success(null!);
    }

    private static string Capitalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        input = input.Trim().ToLower(CultureInfo.InvariantCulture);
        return char.ToUpper(input[0], CultureInfo.InvariantCulture) + input[1..];
    }

    private static RegisterResult CleanPhone(string phone, out string? cleanPhone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            cleanPhone = null;
            return new RegisterResult.InvalidData("Phone number cannot be empty.");
        }

        bool hasLeadingPlus = phone.StartsWith('+');
        phone = PhoneTrim().Replace(phone, "");
        if (hasLeadingPlus)
            phone = "+" + phone;

        if (!Phone().IsMatch(phone))
        {
            cleanPhone = null;
            return new RegisterResult.InvalidData("Phone number contains invalid characters.");
        }

        // Normalize to +31
        if (phone.StartsWith("00")) phone = phone[2..];
        if (phone.StartsWith('+')) phone = phone[1..];
        if (phone.StartsWith("31")) phone = phone[2..];

        if (!PhoneDigits().IsMatch(phone))
        {
            cleanPhone = null;
            return new RegisterResult.InvalidData("Invalid Dutch phone number digits.");
        }

        if (phone.StartsWith('0')) phone = phone[1..];

        cleanPhone = "+31" + phone;
        return new RegisterResult.Success(null!);
    }

    private static RegisterResult CleanEmail(string email, out string? cleanEmail)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email cannot be empty.");
        }

        email = email.Trim();

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email must contain exactly one '@' symbol.");
        }

        string local = parts[0];
        string domain = parts[1];

        try
        {
            var idn = new IdnMapping();
            domain = idn.GetAscii(domain);
        }
        catch (ArgumentException)
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email contains invalid international domain name.");
        }

        string normalizedEmail = $"{local}@{domain.ToLowerInvariant()}";

        if (!EmailRegex().IsMatch(normalizedEmail))
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email address is not in a valid format.");
        }

        cleanEmail = normalizedEmail;
        return new RegisterResult.Success(null!);
    }
}
